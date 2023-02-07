// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
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
