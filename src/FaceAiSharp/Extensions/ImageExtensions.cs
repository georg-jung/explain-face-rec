// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
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
                var cropArea = sourceImage.Bounds();
                cropArea.Intersect(sourceArea);
                op.Crop(cropArea);

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

                var center = RectangleF.Center(faceArea);
                var minSuperSquare = faceArea.GetMinimumSupersetSquare();

                var atb = new AffineTransformBuilder();
                atb.AppendRotationDegrees(angle, center);
                atb.AppendTranslation(new PointF(-minSuperSquare.X, -minSuperSquare.Y));
                op.Transform(atb);

                var squareEdge = minSuperSquare.Height;
                var cropArea = new Rectangle(Point.Empty, op.GetCurrentSize());
                cropArea.Intersect(new Rectangle(0, 0, squareEdge, squareEdge));
                op.Crop(cropArea);

                if (cropArea != minSuperSquare)
                {
                    op.Resize(new ResizeOptions()
                    {
                        Position = AnchorPositionMode.TopLeft,
                        Mode = ResizeMode.BoxPad,
                        PadColor = Color.Black,
                        Size = new Size(squareEdge),
                    });
                }
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

        /// <summary>
        /// Returns an image that matches a defined size and PixelFormat. If the given image already conforms to this specification,
        /// it is returned directly. If a conversion is required the pixels of the input image will only be copied exactly once.
        /// If a copy is created, the <see cref="IDisposable"/> value returned equals the <see cref="Image{TPixel}"/> value in the
        /// same tuple. If the passed-in <see cref="Image"/> is returned directly, the <see cref="IDisposable"/> value returned
        /// is null. Thus, you should always use the <see cref="IDisposable"/> in a <c>using</c> block or
        /// <c>using var</c> declaration.
        /// </summary>
        /// <example>
        /// <code>
        /// (var img, var disp) = image.GetProperlySized&lt;Rgb24&gt;(resizeOptions);
        /// using var usingDisp = disp;
        /// </code>
        /// </example>
        /// <typeparam name="TPixel">The pixel format the returned image should have.</typeparam>
        /// <param name="img">The image to return in a proper shape.</param>
        /// <param name="resizeOptions">How to resize the input, if required.</param>
        /// <param name="throwIfResizeRequired">If an actual Resize operation is required to match the spec, throw.</param>
        /// <returns>An <see cref="Image{TPixel}"/> instance sticking to the spec.</returns>
        public static (Image<TPixel> Image, IDisposable? ToDispose) EnsureProperlySized<TPixel>(this Image img, ResizeOptions resizeOptions, bool throwIfResizeRequired)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            static (Image<TPixel> Image, IDisposable? ToDispose) CreateDisposableTuple(Image<TPixel> img) => (img, img);

            void PerformResize(IImageProcessingContext op) => op.Resize(resizeOptions);

            Image<TPixel> CreateProperSizedImageSameFormat(Image<TPixel> img) => img.Clone(PerformResize);

            Image<TPixel> CreateProperSizedImage(Image img)
            {
                var ret = img.CloneAs<TPixel>();
                ret.Mutate(PerformResize);
                return ret;
            }

            var (wR, hR) = (resizeOptions.Size.Width, resizeOptions.Size.Height); // r = required
            var (wA, hA) = (img.Width, img.Height); // a = actual
            return img switch
            {
                Image<TPixel> rgbImg when wA == wR && hA == hR => (rgbImg, null),
                Image<TPixel> rgbImg when !throwIfResizeRequired => CreateDisposableTuple(CreateProperSizedImageSameFormat(rgbImg)),
                Image when wA == wR && hA == hR => CreateDisposableTuple(img.CloneAs<TPixel>()),
                Image when !throwIfResizeRequired => CreateDisposableTuple(CreateProperSizedImage(img)),
                _ => throw new ArgumentException($"The given image does not have the required dimensions (Required: W={wR}, H={hR}; Actual: W={wA}, H={hA})"),
            };
        }

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
