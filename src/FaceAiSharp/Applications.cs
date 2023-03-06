// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FaceAiSharp.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FaceAiSharp;

public static class Applications
{
    public static void BlurFaces(this IFaceDetector detector, Image<Rgb24> input, float blurSigmaFactor = 10f)
    {
        var res = detector.Detect(input);
        input.Mutate(op =>
        {
            foreach (var fc in res)
            {
                var r = Rectangle.Round(fc.Box);
                r.Intersect(input.Bounds());
                var max = Math.Max(r.Width, r.Height);
                var sigma = Math.Max(max / blurSigmaFactor, blurSigmaFactor);
                op.GaussianBlur(sigma, r);
            }
        });
    }
}
