// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;

namespace FaceAiSharp.Extensions;

public static class GeometryExtensions
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

    public static Rectangle Scale(this Rectangle rectangle, double factor)
        => new(
            (int)(rectangle.X * factor),
            (int)(rectangle.Y * factor),
            (int)(rectangle.Width * factor),
            (int)(rectangle.Height * factor));

    public static Size Scale(this Size size, double factor)
        => new(
            (int)(size.Width * factor),
            (int)(size.Height * factor));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float TwoNorm(this float[] vector)
    {
        double sum = 0;
        foreach (var x in vector)
        {
            sum += x * x;
        }

        return (float)Math.Sqrt(sum);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[] ToUnitLength(this float[] vector)
    {
        var len = vector.TwoNorm();
        var scaled = new float[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            scaled[i] = vector[i] / len;
        }

        return scaled;
    }

    // Euclidean inspired by
    // https://github.com/accord-net/framework/blob/1ab0cc0ba55bcc3d46f20e7bbe7224b58cd01854/Sources/Accord.Math/Distances/Euclidean.cs

    /// <summary>
    /// Calculate euclidean distance between to arbitrary length vectors. Not optimized.
    /// See e.g. https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.vector256-1?view=net-7.0
    /// for possible optimizations. Also, this could use Generic Math if it was .Net 7.
    /// See https://learn.microsoft.com/en-us/dotnet/standard/generics/math.
    /// </summary>
    /// <param name="x">Vector of single precision floating point numbers.</param>
    /// <param name="y">Vector of single precision floating point numbers. Length needs to match the other vector's length.</param>
    /// <returns>Euclidean distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EuclideanDistance(this float[] x, float[] y)
    {
        if (x.Length != y.Length)
        {
            throw new ArgumentException("The Length of the two float[] arrays must match.", nameof(y));
        }

        var l = x.Length;
        float sum = 0.0f;
        for (var i = 0; i < l; i++)
        {
            var dist = x[i] - y[i];
            sum += dist * dist;
        }

        return (float)Math.Sqrt(sum);
    }

    /// <summary>
    /// Gets a similarity measure between two points based on euclidean distance.
    /// </summary>
    /// <param name="x">The first vector to be compared.</param>
    /// <param name="y">The second vector to be compared.</param>
    /// <returns>A similarity measure between x and y.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EuclideanSimilarity(this float[] x, float[] y)
        => 1.0f / (1.0f + EuclideanDistance(x, y));

    // Cosine inspired by
    // https://github.com/accord-net/framework/blob/1ab0cc0ba55bcc3d46f20e7bbe7224b58cd01854/Sources/Accord.Math/Distances/Cosine.cs

    /// <summary>
    ///   Computes the distance <c>d(x,y)</c> between points
    ///   <paramref name="x"/> and <paramref name="y"/>.
    /// </summary>
    /// <param name="x">The first point, x.</param>
    /// <param name="y">The second point, y.</param>
    /// <returns>
    ///   A single-precision value representing the distance <c>d(x,y)</c>
    ///   between <paramref name="x"/> and <paramref name="y"/> according
    ///   to cosine similarity.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CosineDistance(this float[] x, float[] y)
    {
        var sim = CosineSimilarity(x, y);
        return sim == 0 ? 1 : 1 - sim;
    }

    /// <summary>
    /// Gets a similarity measure between two points based on cosine similarity.
    /// </summary>
    /// <param name="x">The first vector to be compared.</param>
    /// <param name="y">The second vector to be compared.</param>
    /// <returns>A similarity measure between x and y.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CosineSimilarity(this float[] x, float[] y)
    {
        double sum = 0;
        double p = 0;
        double q = 0;

        for (var i = 0; i < x.Length; i++)
        {
            sum += x[i] * y[i];
            p += x[i] * x[i];
            q += y[i] * y[i];
        }

        double den = Math.Sqrt(p) * Math.Sqrt(q);
        return (float)((sum == 0) ? 0 : sum / den);
    }
}
