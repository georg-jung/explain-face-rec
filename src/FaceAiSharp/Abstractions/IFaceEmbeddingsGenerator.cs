// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceAiSharp.Abstractions;

public interface IFaceEmbeddingsGenerator
{
    /// <summary>Generate a vectors that are geometrically closer to other vectors returned by this function if the given images belong to the same person.</summary>
    /// <param name="alignedImages">One or more aligned, cropped images of faces.</param>
    /// <returns>Embedding vectors that correspond to the given faces.</returns>
    IEnumerable<float[]> Generate(IReadOnlyList<Image<Rgb24>> alignedImages);

    public float[] Generate(Image<Rgb24> image) => Generate(new[] { image }).First();
}
