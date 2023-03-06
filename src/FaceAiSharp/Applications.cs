// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FaceAiSharp.Abstractions;
using FaceAiSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FaceAiSharp;

public static class Applications
{
    /// <summary>
    /// Blurs all faces that are found by the given face detector. Modifies the given image (destructive).
    /// </summary>
    /// <param name="detector">The face detector to use.</param>
    /// <param name="input">The image to search and blur faces in.</param>
    /// <param name="blurSigmaFactor">Factor to determine sigma for the gaussian blur. sigma = max(height, width) / factor.</param>
    /// <returns>The number of faces that have been blurred.</returns>
    public static int BlurFaces(this IFaceDetector detector, Image<Rgb24> input, float blurSigmaFactor = 10f)
    {
        var res = detector.Detect(input);
        var cnt = 0;
        input.Mutate(op =>
        {
            foreach (var fc in res)
            {
                var r = Rectangle.Round(fc.Box);
                r.Intersect(input.Bounds());
                var max = Math.Max(r.Width, r.Height);
                var sigma = Math.Max(max / blurSigmaFactor, blurSigmaFactor);
                op.GaussianBlur(sigma, r);
                cnt++;
            }
        });
        return cnt;
    }

    public static void CropProfilePicture(this IFaceDetector detector, Image<Rgb24> input, int? maxEdgeSize = 640, float scaleFactor = 1.35f)
    {
        var res = detector.Detect(input);
        if (res.Count == 0)
        {
            throw new ArgumentException("No faces could be found in the given image", nameof(input));
        }

        var maxFace = res.MaxBy(x => x.Confidence);
        var r = Rectangle.Round(maxFace.Box);
        r = r.ScaleCentered(scaleFactor);
        r.Intersect(input.Bounds());
        var angl = 0.0f;
        if (maxFace.Landmarks is not null)
        {
            angl = detector.GetFaceAlignmentAngle(maxFace.Landmarks);
        }

        input.Mutate(op =>
        {
            op.CropAligned(r, angl, maxEdgeSize);
        });
    }
}
