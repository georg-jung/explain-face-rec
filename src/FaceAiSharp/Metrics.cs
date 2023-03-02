// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FaceAiSharp.Extensions;
using SixLabors.ImageSharp;

namespace FaceAiSharp;

public static class Metrics
{
    public static float Auc(IReadOnlyList<(float Confidence, bool IsMatch)> estimations)
    {
        var auc = 0.0;
        var height = 0.0;
        var idx = 0;
        var curPos = 0;
        double p = estimations.Count(x => x.IsMatch);
        double n = estimations.Count - p;
        foreach (var (c, m) in estimations)
        {
            idx++;

            if (m)
            {
                curPos++;
                height += 1 / p;
            }
            else
            {
                auc += height * /* fpr */ (1 - ((idx - curPos) / n));
                height = 0;
            }
        }

        return (float)auc;
    }

    public static IEnumerable<(float X_FPR, float Y_TPR, float Threshold)> RocPoints(IReadOnlyList<(float Confidence, bool IsMatch)> estimations)
    {
        var idx = 0;
        var curPos = 0;
        float p = estimations.Count(x => x.IsMatch);
        float n = estimations.Count - p;
        foreach (var (c, m) in estimations)
        {
            idx++;

            if (m)
            {
                curPos++;
            }

            yield return ((idx - curPos) / n, curPos / p, c);
        }
    }

    public static float FindThreshold(IReadOnlyList<(float Confidence, bool IsMatch)> estimations)
    {
        var idx = 0;
        var tp = 0;
        float p = estimations.Count(x => x.IsMatch);
        float n = estimations.Count - p;
        float pivot = 0.0f;
        var acc = 0.0;
        foreach (var (c, m) in estimations)
        {
            idx++;

            if (m)
            {
                tp++;
            }

            var fp = idx - tp;
            var tn = n - fp;
            var i_acc = (tp + tn) / (p + n);
            if (i_acc > acc)
            {
                acc = i_acc;
                pivot = c;
            }
        }

        return (float)pivot;
    }
}
