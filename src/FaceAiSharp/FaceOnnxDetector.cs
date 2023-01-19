// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FaceAiSharp.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceAiSharp;

internal class FaceOnnxDetector : IFaceDetector
{
    IReadOnlyCollection<(Rectangle Box, float? Confidence)> IFaceDetector.Detect(Image<RgbaVector> image)
    {
        
        throw new NotImplementedException();
    }
}
