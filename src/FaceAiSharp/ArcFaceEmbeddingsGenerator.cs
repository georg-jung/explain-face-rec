// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    public ArcFaceEmbeddingsGenerator(ArcFaceEmbeddingsGeneratorOptions options)
    {
        _session = new(options.ModelPath);
        Options = options;
    }

    public ArcFaceEmbeddingsGeneratorOptions Options { get; }

    public void Dispose() => _session.Dispose();

    public float[] Generate(Image image)
    {
        (var img, var disp) = image.EnsureProperlySized<Rgb24>(_resizeOptions, !Options.AutoResizeInputToModelDimensions);
        using var usingDisp = disp;

        var input = CreateImageTensor(img);

        var inputMeta = _session.InputMetadata;
        var name = inputMeta.Keys.ToArray()[0];

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, input) };
        using var outputs = _session.Run(inputs);
        var dataOutput = outputs.First().AsEnumerable<float>().ToArray();
        var embeddings = dataOutput.ToUnitLength();

        return embeddings;
    }

    private static DenseTensor<float> CreateImageTensor(Image<Rgb24> img)
    {
        var ret = new DenseTensor<float>(new[] { 1, 3, 112, 112 });

        img.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgb24> pixelSpan = accessor.GetRowSpan(y);
                for (var x = 0; x < accessor.Width; x++)
                {
                    ret[0, 0, y, x] = pixelSpan[x].R;
                    ret[0, 1, y, x] = pixelSpan[x].G;
                    ret[0, 2, y, x] = pixelSpan[x].B;
                }
            }
        });

        return ret;
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
