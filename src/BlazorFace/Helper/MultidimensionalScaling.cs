// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using SimpleSimd;

namespace BlazorFace.Helper;

// We use upper case letters for matrix names
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

/// <summary>
/// Translated from https://github.com/scikit-learn/scikit-learn/blob/9670b4243dce06255b341cb3aa613c0da0aeb212/sklearn/manifold/_mds.py and simplified.
/// </summary>
public static class MultidimensionalScaling
{
    public static float[,] DotProductDistanceMatrix(IReadOnlyList<float[]> data)
    {
        int n = data.Count;
        var distanceMatrix = new float[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                float dot = SimdOps<float>.Dot(data[i], data[j]);
                var distance = dot <= 0 ? 1 : 1 - dot;
                distanceMatrix[i, j] = distance;
                distanceMatrix[j, i] = distance;
            }
        }

        return distanceMatrix;
    }

    public static (float[][] Positions, float Stress) Smacof(float[,] distanceMatrix, int targetDimension, int iterations = 12)
    {
        var winner = Enumerable.Range(0, iterations).AsParallel()
            .Select(i => SmacofSingle(distanceMatrix, targetDimension))
            .Min();
        return (winner.Positions.ToRowArrays(), winner.Stress);
    }

    private static (float Stress, Matrix<float> Positions) SmacofSingle(float[,] distanceMatrix, int targetDimension)
    {
        int n_samples = distanceMatrix.GetLength(0);

        // Randomly choose initial configuration
        var X = Matrix<float>.Build.Random(n_samples, targetDimension);

        float old_stress = float.NaN;
        int it;
        for (it = 0; it < 300; it++)
        {
            var D = DistanceMatrix(X);
            var disparities = DenseMatrix.OfArray(distanceMatrix);

            var stress = (D - disparities).PointwisePower(2).RowSums().Sum() / 2;

            // Update X using the Guttman transform
            D = D.PointwiseMaximum(1e-5f); // dis[dis == 0] = 1e-5
            var ratio = disparities.PointwiseDivide(D);
            var B = -ratio;
            for (int i = 0; i < n_samples; i++)
            {
                B[i, i] += ratio.Row(i).Sum();
            }

            X = 1.0f / n_samples * B * X;

            float dis = X.PointwisePower(2).RowSums().PointwiseSqrt().Sum();
            if (!float.IsNaN(old_stress))
            {
                if ((old_stress - (stress / dis)) < 1e-3f)
                {
                    break;
                }
            }

            old_stress = stress / dis;
        }

        return (old_stress, X);
    }

    private static Matrix<float> DistanceMatrix(Matrix<float> X)
    {
        var D = DenseMatrix.Create(X.RowCount, X.RowCount, 0.0f);
        for (int i = 0; i < X.RowCount; i++)
        {
            for (int j = i + 1; j < X.RowCount; j++)
            {
                var d = (float)X.Row(i).Subtract(X.Row(j)).L2Norm();
                D[i, j] = d;
                D[j, i] = d;
            }
        }

        return D;
    }
}
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
