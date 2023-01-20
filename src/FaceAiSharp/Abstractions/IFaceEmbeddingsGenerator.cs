// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SixLabors.ImageSharp;

namespace FaceAiSharp.Abstractions;

public interface IFaceEmbeddingsGenerator
{
    /// <summary>Generate a vector that is geometrically closer to other vectors returned by this function if the given images belong to the same person.</summary>
    /// <param name="image">An aligned, cropped image of a face.</param>
    /// <returns>An embedding vector that corresponds to the given face.</returns>
    float[] Generate(Image image);
}
