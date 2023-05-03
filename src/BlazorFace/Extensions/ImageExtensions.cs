// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Accord.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace BlazorFace.Extensions;

internal static class ImageExtensions
{
    /// <summary>
    /// Draws multiple rectangles onto a given image, e.g. to demonstrate where faces were detected in a picture.
    /// The given image is mutated.
    /// </summary>
    /// <param name="image">The image to draw the rectangles onto.</param>
    /// <param name="brush">The brush to draw the lines with.</param>
    /// <param name="toDraw">An enumeration of the rectangles to draw.</param>
    /// <param name="thickness">The thickness to draw the lines in. Chosen automatically based on image dimensions if null.</param>
    /// <remarks>This is a destructive operation.</remarks>
    public static void DrawRectangles(this Image image, IBrush brush, IEnumerable<Rectangle> toDraw, float? thickness = null)
        => image.Mutate(op =>
        {
            var tn = thickness ?? Math.Max(image.Width / 400f, 1);
            foreach (var rect in toDraw)
            {
                op.Draw(brush, tn, rect);
            }
        });

    /// <summary>
    /// Draws multiple points onto a given image, e.g. to demonstrate where facial landmarks were detected in a picture.
    /// </summary>
    /// <param name="image">The image to draw the points onto.</param>
    /// <param name="brush">The brush to draw the points with.</param>
    /// <param name="toDraw">An enumeration of the points to draw.</param>
    /// <returns>A copy of the given image with the points drawn onto.</returns>
    public static Image DrawPoints(this Image image, IBrush brush, IEnumerable<Point> toDraw)
        => image.Clone(op =>
        {
            var delta = Math.Max(image.Width / 400, 1);
            foreach (var pt in toDraw)
            {
                var rect = new Rectangle() { X = pt.X - delta, Y = pt.Y - delta, Height = 2 * delta, Width = 2 * delta };
                op.Fill(brush, rect);
            }
        });

    /// <summary>
    /// Draws multiple rectangles and points onto a given image, e.g. to demonstrate where faces and their landmarks were detected in a picture.
    /// </summary>
    /// <param name="image">The image to draw onto.</param>
    /// <param name="brush">The brush to draw with.</param>
    /// <param name="rectsToDraw">An enumeration of the rectangles to draw.</param>
    /// <param name="pointsToDraw">An enumeration of the points to draw.</param>
    /// <returns>A copy of the given image with the points drawn onto.</returns>
    public static Image DrawRectanglesAndPoints(this Image image, IBrush brush, IEnumerable<RectangleF> rectsToDraw, IEnumerable<PointF> pointsToDraw)
        => image.Clone(op =>
        {
            var delta = Math.Max(image.Width / 400, 1);
            foreach (var rect in rectsToDraw)
            {
                op.Draw(brush, delta, rect);
            }

            foreach (var pt in pointsToDraw)
            {
                var rect = new RectangleF() { X = pt.X - delta, Y = pt.Y - delta, Height = 2 * delta, Width = 2 * delta };
                op.Fill(brush, rect);
            }
        });

    // Can this be optimized by https://github.com/SixLabors/ImageSharp/discussions/1666 ?
    public static Image Extract(this Image sourceImage, Rectangle sourceArea) => sourceImage.Clone(op => op.Crop(sourceArea));

    public static Image Extract(this Image sourceImage, Rectangle sourceArea, int extractedMaxEdgeSize, bool scaleUpToMaxEdgeSize = false)
        => sourceImage.Clone(op =>
        {
            var longestDim = Math.Max(sourceArea.Width, sourceArea.Height);
            var tooLargeFactor = longestDim / (double)extractedMaxEdgeSize;
            var factor = 1.0 / tooLargeFactor; // scale factor

            // cropping before resizing is much faster, see benchmarks
            var cropArea = sourceImage.Bounds();
            cropArea.Intersect(sourceArea);
            op.Crop(cropArea);

            if (factor < 1 || scaleUpToMaxEdgeSize)
            {
                var curSize = op.GetCurrentSize();
                op.Resize(curSize.Scale(factor));
            }
        });
}
