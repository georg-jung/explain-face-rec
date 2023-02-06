// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceAiSharp.Extensions;

public static class ImageToTensorExtensions
{
    /* this is heavily based on https://github.com/skywalkerisnull/onnxruntime-csharp-cv-template/blob/bb1454a51a722e293a918d2b5a25abda864f9f74/utils/ImageHelper.cs#L62
     * see also https://github.com/SixLabors/ImageSharp/discussions/1955
     */

    public static DenseTensor<float> ToTensor(this Image<Rgb24> image)
    {
        var mean = new[] { 0.5f, 0.5f, 0.5f };
        var stddev = new[] { 1f, 1f, 1f };
        var dims = new[] { 1, 3, image.Height, image.Width };
        return ImageToTensor(new[] { image }, mean, stddev, dims);
    }

    public static DenseTensor<float> ToTensor(this Image<Rgb24> image, float[] mean, float[] stddev, int[] inputDimension)
    {
        return ImageToTensor(new[] { image }, mean, stddev, inputDimension);
    }

    /// <summary>
    /// Efficiently converts images to a input tensor for batch processing.
    /// </summary>
    /// <param name="images">The images to convert.</param>
    /// <param name="mean">The rgb mean values used for normalization.</param>
    /// <param name="stddev">The rgb stddev values used for normalization.</param>
    /// <param name="inputDimension">The size of the tensor that the OnnxRuntime model is expecting, e.g. [1, 3, 224, 224].</param>
    /// <returns>A tensor that contains the converted batch of images.</returns>
    public static DenseTensor<float> ImageToTensor(IReadOnlyCollection<Image<Rgb24>> images, float[] mean, float[] stddev, int[] inputDimension)
    {
        var strides = GetStrides(inputDimension);

        // Calculate these outside the loop
        var normR = mean[0] / stddev[0];
        var normG = mean[1] / stddev[1];
        var normB = mean[2] / stddev[2];

        var stdNormR = 1 / (255f * stddev[0]);
        var stdNormG = 1 / (255f * stddev[1]);
        var stdNormB = 1 / (255f * stddev[2]);

        inputDimension[0] = images.Count;

        var input = new DenseTensor<float>(inputDimension);

        foreach (var image in images)
        {
            image.ProcessPixelRows(pixelAccessor =>
            {
                var inputSpan = input.Buffer.Span;
                for (var y = 0; y < image.Height; y++)
                {
                    var index = y * strides[2];
                    var rowSpan = pixelAccessor.GetRowSpan(y);

                    // Faster indexing into the span
                    var spanR = inputSpan.Slice(index, rowSpan.Length);
                    index += strides[1];
                    var spanG = inputSpan.Slice(index, rowSpan.Length);
                    index += strides[1];
                    var spanB = inputSpan.Slice(index, rowSpan.Length);

                    // Now we can just directly loop through and copy the values directly from span to span.
                    for (int x = 0; x < rowSpan.Length; x++)
                    {
                        spanR[x] = (rowSpan[x].R * stdNormR) - normR;
                        spanG[x] = (rowSpan[x].G * stdNormG) - normG;
                        spanB[x] = (rowSpan[x].B * stdNormB) - normB;
                    }
                }
            });
        }

        return input;
    }

    /// <summary>
    /// Gets the set of strides that can be used to calculate the offset of n-dimensions in a 1-dimensional layout.
    /// </summary>
    private static int[] GetStrides(ReadOnlySpan<int> dimensions, bool reverseStride = false)
    {
        int[] strides = new int[dimensions.Length];
        if (dimensions.Length == 0)
        {
            return strides;
        }

        int stride = 1;
        if (reverseStride)
        {
            for (int i = 0; i < strides.Length; i++)
            {
                strides[i] = stride;
                stride *= dimensions[i];
            }
        }
        else
        {
            for (int i = strides.Length - 1; i >= 0; i--)
            {
                strides[i] = stride;
                stride *= dimensions[i];
            }
        }

        return strides;
    }
}
