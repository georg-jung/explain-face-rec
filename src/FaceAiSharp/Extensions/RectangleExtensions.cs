// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SixLabors.ImageSharp;

namespace FaceAiSharp.Extensions;

public static class RectangleExtensions
{
    /// <summary>
    /// Gets an area that contains all pixels that could be needed if the given crop-rectangle should be rotated by any angle.
    /// </summary>
    /// <param name="rectangle">A crop rectangle.</param>
    /// <returns>A larger rectangle.</returns>
    public static Rectangle ScaleToRotationAngleInvariantCropArea(this Rectangle rectangle)
    {
        // adapted from https://github.com/FaceONNX/FaceONNX/blob/aa6943be0831bee06b16d317c4f3fd0888480049/netstandard/FaceONNX/face/utils/Rectangles.cs#L349
        var r = (int)Math.Sqrt((rectangle.Width * rectangle.Width) + (rectangle.Height * rectangle.Height));
        var dx = r - rectangle.Width;
        var dy = r - rectangle.Height;

        var x = rectangle.X - (dx / 2);
        var y = rectangle.Y - (dy / 2);
        var w = rectangle.Width + dx;
        var h = rectangle.Height + dy;

        return new Rectangle
        {
            X = x,
            Y = y,
            Width = w,
            Height = h,
        };
    }
}
