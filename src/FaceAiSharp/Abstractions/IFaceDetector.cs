// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceAiSharp.Abstractions;

public interface IFaceDetector
{
    IReadOnlyCollection<(Rectangle Box, float? Confidence)> Detect(Image<RgbaVector> image);
}
