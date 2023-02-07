// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FaceAiSharp.Abstractions;
using FaceAiSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceAiSharp;

public sealed class FaceOnnxEmbeddingsGenerator : IFaceEmbeddingsGenerator, IDisposable
{
    private readonly FaceONNX.FaceEmbedder _fonnx = new();

    public void Dispose() => _fonnx.Dispose();

    public IEnumerable<float[]> Generate(IReadOnlyList<Image<Rgb24>> alignedImages)
    {
        foreach (var image in alignedImages)
        {
            yield return Generate(image);
        }
    }

    public float[] Generate(Image<Rgb24> image)
    {
        var img = image.ToFaceOnnxFloatArray();
        var res = _fonnx.Forward(img);
        return res;
    }
}
