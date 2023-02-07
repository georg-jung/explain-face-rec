// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    public float[] Generate(Image image)
    {
        (var img, var disp) = image.EnsureProperlySized<Rgb24>(_resizeOptions, !Options.AutoResizeInputToModelDimensions);
        using var usingDisp = disp;

        var input = CreateImageTensor(img);

        var inputMeta = _session.InputMetadata;
        var name = inputMeta.Keys.First();

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, input) };
        using var outputs = _session.Run(inputs);
        var dataOutput = outputs.First().AsEnumerable<float>().ToArray();
        var embeddings = dataOutput.ToUnitLength();

        return embeddings;
    }

    internal static DenseTensor<float> CreateImageTensor(Image<Rgb24> img)
    {
        // ArcFace uses the rgb values directly, just the ints converted to float,
        // no further preprocessing needed. The default ToTensor implementation assumes
        // we want the RGB[
        var mean = new[] { 0f, 0f, 0f };
        var stdDevVal = 1 / 255f;
        var stdDev = new[] { stdDevVal, stdDevVal, stdDevVal };
        return img.ToTensor(mean, stdDev);
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

    public static void AlignUsingFacialLandmarks(Image face, IReadOnlyList<PointF> landmarks)
    {
        var m = EstimateAffineAlignmentMatrix(landmarks);
        face.Mutate(op =>
        {
            var afb = new AffineTransformBuilder();
            afb.AppendMatrix(m);
            op.Transform(afb);
        });
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
