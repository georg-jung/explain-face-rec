// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FaceAiSharp.Abstractions;
using FaceAiSharp.Extensions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UMapx.Core;
using UMapx.Imaging;

namespace FaceAiSharp;

public sealed class ArcFaceEmbeddingsGenerator : IFaceEmbeddingsGenerator, IDisposable
{
    private readonly InferenceSession _session;

    public ArcFaceEmbeddingsGenerator()
    {
        _session = new(@"C:\Users\georg\facePics\arcfaceresnet100-8\resnet100\resnet100.onnx");
    }

    public void Dispose() => _session.Dispose();

    public float[] Generate(Image image)
    {
        var img = image.ToSquare(112).CloneAs<Rgb24>();

        Tensor<float> input = new DenseTensor<float>(new[] { 1, 3, 112, 112 });

        img.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgb24> pixelSpan = accessor.GetRowSpan(y);
                for (var x = 0; x < accessor.Width; x++)
                {
                    input[0, 0, y, x] = pixelSpan[x].R;
                    input[0, 1, y, x] = pixelSpan[x].G;
                    input[0, 2, y, x] = pixelSpan[x].B;
                }
            }
        });

        var inputMeta = _session.InputMetadata;
        var name = inputMeta.Keys.ToArray()[0];

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, input) };
        using var outputs = _session.Run(inputs);
        var dataOutput = outputs.First().AsEnumerable<float>().ToArray();
        var embeddings = dataOutput.ToUnitLength();

        return embeddings;
    }
}
