// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using FaceONNX;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace FaceAiSharp.Extensions
{
    public static class ImageExtensions
    {
        // Can this be optimized by https://github.com/SixLabors/ImageSharp/discussions/1666 ?
        public static Image Extract(this Image sourceImage, Rectangle sourceArea) => sourceImage.Clone(op => op.Crop(sourceArea));

        public static Image Extract(this Image sourceImage, Rectangle sourceArea, int extractedMaxEdgeSize)
            => sourceImage.Clone(op =>
            {
                var longestDim = Math.Max(sourceArea.Width, sourceArea.Height);
                var toLargeFactor = Math.Max(1.0, longestDim / (double)extractedMaxEdgeSize);
                var factor = 1.0 / toLargeFactor; // scale factor

                // cropping before resizing is much faster, see benchmarks
                op.Crop(sourceArea);

                if (factor < 1)
                {
                    var curSize = op.GetCurrentSize();
                    op.Resize(curSize.Scale(factor));
                }
            });

        public static Image ToSquare(this Image sourceImage, int maxEdgeSize)
            => ToSquare(sourceImage, maxEdgeSize, Color.White);

        public static Image ToSquare(this Image sourceImage, int maxEdgeSize, Color padColor)
            => sourceImage.Clone(op =>
            {
                var opts = new ResizeOptions()
                {
                    Mode = ResizeMode.Pad,
                    PadColor = padColor,
                    Size = new Size(maxEdgeSize),
                };
                op.Resize(opts);
            });

        public static Image CropAligned(this Image sourceImage, Rectangle faceArea, float angle, int? alignedMaxEdgeSize = 250)
            => sourceImage.Clone(op =>
            {
                if (alignedMaxEdgeSize.HasValue)
                {
                    var longestDim = Math.Max(faceArea.Width, faceArea.Height);
                    var toLargeFactor = Math.Max(1.0, longestDim / (double)alignedMaxEdgeSize);
                    var factor = 1.0 / toLargeFactor; // scale factor

                    if (factor < 1)
                    {
                        var curSize = op.GetCurrentSize();
                        op.Resize(curSize.Scale(factor));
                        faceArea = faceArea.Scale(factor);
                    }
                }

                var angleInvariantCropArea = faceArea.ScaleToRotationAngleInvariantCropArea();

                var imgSz = op.GetCurrentSize();
                var imgRect = new Rectangle(0, 0, imgSz.Width, imgSz.Height);
                var usedCropArea = false;
                if (imgRect.Contains(angleInvariantCropArea))
                {
                    op.Crop(angleInvariantCropArea);
                    usedCropArea = true;
                }

                op.Rotate(angle);

                // We have cropped above to an area that is larger than our actual face area.
                // It is exactly so large that it fits every possible rotation of the given face
                // area around any angle, rotated around it's center. Thus, we don't have black/blank
                // areas after applying the rotation. Now, we do want to crop the rotated image to our
                // actual faceArea.
                var cropAreaAfterRotation = new Rectangle()
                {
                    X = faceArea.X - (usedCropArea ? angleInvariantCropArea.X : 0),
                    Y = faceArea.Y - (usedCropArea ? angleInvariantCropArea.Y : 0),
                    Height = faceArea.Height,
                    Width = faceArea.Width,
                };
                op.Crop(cropAreaAfterRotation);
            });

        /// <summary>
        /// Draws multiple rectangles onto a given image, e.g. to demonstrate where faces were detected in a picture.
        /// </summary>
        /// <param name="image">The image to draw the rectangles onto.</param>
        /// <param name="brush">The brush to draw the lines with.</param>
        /// <param name="toDraw">An enumeration of the rectangles to draw.</param>
        /// <param name="thickness">The thickness to draw the lines in.</param>
        /// <returns>A copy of the given image with the rectangles drawn onto.</returns>
        public static Image DrawRectangles(this Image image, IBrush brush, IEnumerable<Rectangle> toDraw, float thickness = 1.0f)
            => image.Clone(op =>
            {
                foreach (var rect in toDraw)
                {
                    op.Draw(brush, thickness, rect);
                }
            });

        /// <summary>
        /// Draws multiple rectangles onto a given image, e.g. to demonstrate where faces were detected in a picture.
        /// </summary>
        /// <param name="image">The image to draw the rectangles onto.</param>
        /// <param name="brush">The brush to draw the points with.</param>
        /// <param name="toDraw">An enumeration of the rectangles to draw.</param>
        /// <returns>A copy of the given image with the rectangles drawn onto.</returns>
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

        public static float[][,] ToFaceOnnxFloatArray(this Image image)
        {
            var r = new float[image.Height, image.Width];
            var g = new float[image.Height, image.Width];
            var b = new float[image.Height, image.Width];
            image.Mutate(c => c.ProcessPixelRowsAsVector4((row, point) =>
            {
                for (var x = 0; x < row.Length; x++)
                {
                    // Get a reference to the pixel at position x
                    ref var pixel = ref row[x];
                    var y = point.Y;
                    r[y, x] = pixel.X;
                    g[y, x] = pixel.Y;
                    b[y, x] = pixel.Z;
                }
            }));
            return new float[][,] { b, g, r };
        }
    }
}
