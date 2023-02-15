// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using FaceAiSharp.Abstractions;
using FaceAiSharp.Extensions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FaceAiSharp;

public sealed class ArcFaceEmbeddingsGenerator : IFaceEmbeddingsGenerator, IDisposable
{
    // points from https://github.com/deepinsight/insightface/blob/c7bf2048e8947a6398b4b8bda6d1958138fdc9b5/python-package/insightface/utils/face_align.py
    private static readonly IReadOnlyList<PointF> ExpectedLandmarkPositions = new List<PointF>()
    {
        new PointF(38.2946f, 51.6963f),
        new PointF(73.5318f, 51.5014f),
        new PointF(56.0252f, 71.7366f),
        new PointF(41.5493f, 92.3655f),
        new PointF(70.7299f, 92.2041f),
    }.AsReadOnly();

    private static readonly ResizeOptions _resizeOptions = new()
    {
        Mode = ResizeMode.Pad,
        PadColor = Color.White,
        Size = new Size(112, 112),
    };

    private readonly InferenceSession _session;

    public ArcFaceEmbeddingsGenerator(ArcFaceEmbeddingsGeneratorOptions options, SessionOptions? sessionOptions = null)
    {
        Options = options;
        if (sessionOptions is null)
        {
            _session = new(options.ModelPath);
        }
        else
        {
            _session = new(options.ModelPath, sessionOptions);
        }
    }

    public ArcFaceEmbeddingsGeneratorOptions Options { get; }

    public void Dispose() => _session.Dispose();

    public IEnumerable<float[]> Generate(IReadOnlyList<Image<Rgb24>> alignedImages)
    {
        foreach (var img in alignedImages)
        {
            img.EnsureProperlySizedDestructive(_resizeOptions, !Options.AutoResizeInputToModelDimensions);
        }

        var input = CreateImageTensor(alignedImages);

        var inputMeta = _session.InputMetadata;
        var name = inputMeta.Keys.First();

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, input) };
        using var outputs = _session.Run(inputs);
        var firstOut = outputs.First();
        var tens = firstOut.Value as DenseTensor<float> ?? firstOut.AsTensor<float>().ToDenseTensor();
        Debug.Assert(tens.Length % 512 == 0, "Output tensor length is invalid.");

        var embSpan = tens.Buffer.Span;
        var emb = new List<float[]>(alignedImages.Count);
        for (var i = 0; i < alignedImages.Count; i++)
        {
            var span = embSpan.Slice(i * 512, 512);
            emb.Add(GeometryExtensions.ToUnitLength(span));
        }

        return emb;
    }

    /// <summary>
    /// Transform and crop the given image in the way ArcFace was trained.
    /// </summary>
    /// <param name="face">Image containing the face. The given image will be mutated.</param>
    /// <param name="landmarks">5 facial landmark points.</param>
    public static void AlignUsingFacialLandmarks(Image face, IReadOnlyList<PointF> landmarks)
    {
        var cutRect = new Rectangle(0, 0, 112, 112);
        var m = EstimateAffineAlignmentMatrix(landmarks);
        var success = Matrix3x2.Invert(m, out var mi);
        if (!success)
        {
            throw new InvalidOperationException("Could not invert matrix.");
        }

        /* The matrix m transforms the given image in a way that the given landmark points will
         * be projected inside the 112x112 rectangle that is used as input for ArcFace. If the input
         * image is much larger than the face area we are interested in, applying this transform to 
         * the complete image would waste cpu time. Thus we first invert the matrix, project our
         * 112x112 crop area using the matrix' inverse and take the minimum surrounding rectangle
         * of that projection. We crop the image using that rectangle and proceed. */
        var area = cutRect.SupersetAreaOfTransform(mi);

        /* The matrix m includes scaling. If we scale the image using an affine transform,
         * we loose quality because we don't use any specialized resizing methods. Thus, we extract
         * the x and y scale factors from the matrix, scale using Resize first and remove the scaling
         * from m by multiplying it with an inverted scale matrix. */
        var (hScale, vScale) = (m.GetHScaleFactor(), m.GetVScaleFactor());
        var mScale = Matrix3x2.CreateScale(1 / hScale, 1 / vScale);
        face.Mutate(op =>
        {
            SafeCrop(op, area);

            var afb = new AffineTransformBuilder();
            var sz = op.GetCurrentSize();
            var scale = new SizeF(sz.Width * hScale, sz.Height * vScale);
            op.Resize(Size.Round(scale));
            m = Matrix3x2.Multiply(mScale, m);

            // the Crop does the inverse translation so we need to undo it
            afb.AppendTranslation(new PointF(area.X * hScale, area.Y * vScale));
            afb.AppendMatrix(m);
            op.Transform(afb);

            SafeCrop(op, cutRect);
        });
    }

    internal static DenseTensor<float> CreateImageTensor(IReadOnlyCollection<Image<Rgb24>> imgs)
    {
        // ArcFace uses the rgb values directly, just the ints converted to float,
        // no further preprocessing needed. The default ToTensor implementation assumes
        // we want the RGB[
        var mean = new[] { 0f, 0f, 0f };
        var stdDevVal = 1 / 255f;
        var stdDev = new[] { stdDevVal, stdDevVal, stdDevVal };
        var inputDim = new[] { imgs.Count, 3, 112, 112 };
        return ImageToTensorExtensions.ImageToTensor(imgs, mean, stdDev, inputDim);
    }

    internal static System.Numerics.Matrix3x2 EstimateAffineAlignmentMatrix(IReadOnlyList<PointF> landmarks)
    {
        Guard.HasSizeEqualTo(landmarks, 5);
        var estimate = new List<(PointF A, PointF B)>
        {
            (landmarks[0], ExpectedLandmarkPositions[0]),
            (landmarks[1], ExpectedLandmarkPositions[1]),
            (landmarks[2], ExpectedLandmarkPositions[2]),
            (landmarks[3], ExpectedLandmarkPositions[3]),
            (landmarks[4], ExpectedLandmarkPositions[4]),
        };
        var m = estimate.EstimateSimilarityMatrix();
        return m;
    }

    private static void SafeCrop(IImageProcessingContext op, Rectangle rect)
    {
        var sz = op.GetCurrentSize();
        var max = new Rectangle(0, 0, sz.Width, sz.Height);
        max.Intersect(rect);
        op.Crop(max);
    }
}

public record ArcFaceEmbeddingsGeneratorOptions
{
    /// <summary>
    /// Gets the path to the onnx file that contains the resnet100 model with 1x3x112x112 input.
    /// </summary>
    public string ModelPath { get; init; } = default!;

    /// <summary>
    /// Resize the image to dimensions supported by the model if required. This detector throws an
    /// exception if this is set to false and an image is passed in unsupported dimensions.
    /// </summary>
    public bool AutoResizeInputToModelDimensions { get; init; } = true;
}
