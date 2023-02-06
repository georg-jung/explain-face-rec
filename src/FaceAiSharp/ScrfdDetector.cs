// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using FaceAiSharp.Abstractions;
using FaceAiSharp.Extensions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FaceAiSharp;

public sealed class ScrfdDetector : IFaceDetector, IDisposable
{
    private readonly InferenceSession _session;
    private readonly ModelParameters _modelParameters;

    public ScrfdDetector(ScrfdDetectorOptions options)
    {
        Options = options;
        _session = new(options.ModelPath);
        _modelParameters = DetermineModelParameters();
    }

    public ScrfdDetectorOptions Options { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF GetLeftEye(IReadOnlyList<PointF> landmarks) => landmarks[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF GetRightEye(IReadOnlyList<PointF> landmarks) => landmarks[1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF GetNose(IReadOnlyList<PointF> landmarks) => landmarks[2];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF GetMouthLeft(IReadOnlyList<PointF> landmarks) => landmarks[3];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF GetMouthRight(IReadOnlyList<PointF> landmarks) => landmarks[4];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetFaceAlignmentAngle(IReadOnlyList<PointF> landmarks)
    {
        // adapted from https://stackoverflow.com/a/12892493/1200847
        var le = GetLeftEye(landmarks);
        var re = GetRightEye(landmarks);

        var diff = re - le;
        return Math.Atan2(diff.Y, diff.X) * 180.0 / Math.PI * -1;
    }

    public IReadOnlyCollection<FaceDetectorResult> Detect(Image image)
    {
        var resizeOptions = new ResizeOptions()
        {
            Size = _modelParameters.InputSize ?? new Size((int)Math.Ceiling(image.Width / 32.0) * 32, (int)Math.Ceiling(image.Height / 32.0) * 32),
            Position = AnchorPositionMode.TopLeft,
            Mode = ResizeMode.BoxPad,
            PadColor = Color.Black,
        };

        (var img, var disp) = image.EnsureProperlySized<Rgb24>(resizeOptions, !Options.AutoResizeInputToModelDimensions);
        using var usingDisp = disp;
        var scale = 1 / image.Bounds().GetScaleFactorToFitInto(resizeOptions.Size);

        var input = CreateImageTensor(img);

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_modelParameters.InputName, input) };
        using var outputs = _session.Run(inputs);

        List<NDArray> scoresLst = new(3);
        List<NDArray> bboxesLst = new(3);
        List<NDArray> kpssLst = new(3);

        foreach (var stride in new[] { 8, 16, 32 })
        {
            var strideRes = HandleStride(stride, outputs, img.Size(), Options.ConfidenceThreshold, _modelParameters.Batching);
            if (!strideRes.HasValue)
            {
                continue;
            }

            var (s, bb, kps) = strideRes.Value;
            scoresLst.Add(s);
            bboxesLst.Add(bb);
            if (kps is not null)
            {
                kpssLst.Add(kps);
            }
        }

        var scores = np.vstack(scoresLst.ToArray());
        if (scores.size == 0)
        {
            return new List<FaceDetectorResult>(0);
        }

        var scores_ravel = scores.ravel();
        var order = scores_ravel.argsort<float>()["::-1"];
        var bboxes = np.vstack(bboxesLst.ToArray());

        var preDet = np.hstack(bboxes, scores);

        preDet = preDet[order];
        var keep = np.array(NonMaxSupression(preDet, Options.NonMaxSupressionThreshold));
        var det = preDet[keep];
        det *= scale;

        NDArray? kpss = null;
        if (kpssLst.Count > 0)
        {
            kpss = np.vstack(kpssLst.ToArray());
            kpss = kpss[order];
            kpss = kpss[keep];
            kpss *= scale;
        }

        static FaceDetectorResult ToReturnType(NDArray input)
        {
            var x1 = input.GetSingle(0);
            var y1 = input.GetSingle(1);
            var x2 = input.GetSingle(2);
            var y2 = input.GetSingle(3);
            return new(new RectangleF(x1, y1, x2 - x1, y2 - y1), null, input.GetSingle(4));
        }

        static FaceDetectorResult ToReturnTypeWithLandmarks(NDArray input, NDArray kps)
        {
            var (box, _, conf) = ToReturnType(input);
            var lmrks = new List<PointF>(5); // don't use ToList because we know we will always have eactly 5.
            lmrks.AddRange(kps.GetNDArrays(0).Select(x => new PointF(x.GetSingle(0), x.GetSingle(1))));
            return new(box, lmrks, conf);
        }

        if (kpss is not null)
        {
            return det.GetNDArrays(0).Zip(kpss.GetNDArrays(0)).Select(x => ToReturnTypeWithLandmarks(x.First, x.Second)).ToList();
        }
        else
        {
            return det.GetNDArrays(0).Select(ToReturnType).ToList();
        }
    }

    float IFaceDetector.GetFaceAlignmentAngle(IReadOnlyList<PointF> landmarks) => (float)GetFaceAlignmentAngle(landmarks);

    public void Dispose() => _session.Dispose();

    private static DenseTensor<float> CreateImageTensor(Image<Rgb24> img)
    {
        var ret = new DenseTensor<float>(new[] { 1, 3, img.Height, img.Width });

        var mean = new[] { 0.5f, 0.5f, 0.5f };
        img.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgb24> pixelSpan = accessor.GetRowSpan(y);
                for (var x = 0; x < accessor.Width; x++)
                {
                    ret[0, 0, y, x] = (pixelSpan[x].R / 255f) - mean[0];
                    ret[0, 1, y, x] = (pixelSpan[x].G / 255f) - mean[1];
                    ret[0, 2, y, x] = (pixelSpan[x].B / 255f) - mean[2];
                }
            }
        });

        return ret;
    }

    private static NDArray GenerateAnchorCenters(Size inputSize, int stride, int numAnchors)
    {
        // translated from https://github.com/deepinsight/insightface/blob/f091989568cad5a0244e05be1b8d58723de210b0/detection/scrfd/tools/scrfd.py#L185
        var height = inputSize.Height / stride;
        var width = inputSize.Width / stride;
        var (mgrid1, mgrid2) = np.mgrid(np.arange(height), np.arange(width));
        var anchorCenters = np.stack(new[] { mgrid2, mgrid1 }, axis: -1).astype(np.float32);
        anchorCenters = (anchorCenters * stride).reshape(-1, 2);
        if (numAnchors > 1)
        {
            anchorCenters = np.stack(new[] { anchorCenters, anchorCenters }, axis: 1).reshape(-1, 2);
        }

        return anchorCenters;
    }

    /// <summary>
    /// In real numpy this could be eg. np.where(scores>=thresh) or np.asarray(condition).nonzero().
    /// </summary>
    /// <param name="input">The indices of this array's elements should be returned.</param>
    /// <param name="threshold">The threshold value. Exclusive.</param>
    /// <returns>An NDArray contianing the indices.</returns>
    private static NDArray IndicesOfElementsLargerThen(NDArray input, float threshold)
    {
        var zeroIfBelow = np.sign(input - threshold) + 1;
        var ret = np.nonzero(zeroIfBelow);
        return ret[0];
    }

    /// <summary>
    /// In real numpy this could be eg. np.where(scores<=thresh) or np.asarray(condition).nonzero().
    /// </summary>
    /// <param name="input">The indices of this array's elements should be returned.</param>
    /// <param name="threshold">The threshold value. Exclusive.</param>
    /// <returns>An NDArray contianing the indices.</returns>
    private static NDArray IndicesOfElementsBelow(NDArray input, float threshold)
    {
        var zeroIfAbove = np.sign(input - threshold) - 1;
        var ret = np.nonzero(zeroIfAbove);
        return ret[0];
    }

    private static NDArray Distance2Bbox(NDArray points, NDArray distance)
    {
        var x1 = points[":, 0"] - distance[":, 0"];
        var y1 = points[":, 1"] - distance[":, 1"];
        var x2 = points[":, 0"] + distance[":, 2"];
        var y2 = points[":, 1"] + distance[":, 3"];
        return np.stack(new[] { x1, y1, x2, y2 }, axis: -1);
    }

    private static NDArray Distance2Kps(NDArray points, NDArray distance)
    {
        var preds = new NDArray[distance.shape[1]];
        for (var i = 0; i < distance.shape[1]; i += 2)
        {
            var px = points[Slice.All, i % 2] + distance[Slice.All, i];
            var py = points[Slice.All, (i % 2) + 1] + distance[Slice.All, i + 1];
            preds[i] = px;
            preds[i + 1] = py;
        }

        return np.stack(preds, axis: -1);
    }

    private static (NDArray Scores, NDArray Bboxes, NDArray? Kpss)? HandleStride(int stride, IReadOnlyCollection<NamedOnnxValue> outputs, Size inputSize, float thresh, bool batched)
    {
        var bbox_preds = outputs.First(x => x.Name == $"bbox_{stride}").ToNDArray<float>();
        var scores = outputs.First(x => x.Name == $"score_{stride}").ToNDArray<float>();
        var kps_preds = outputs.FirstOrDefault(x => x.Name == $"kps_{stride}")?.ToNDArray<float>();

        if (batched)
        {
            bbox_preds = bbox_preds[0];
            scores = scores[0];
            kps_preds = kps_preds?[0];
        }

        bbox_preds *= stride;
        kps_preds = kps_preds is not null ? kps_preds * stride : null;

        var height = inputSize.Height / stride;
        var width = inputSize.Width / stride;
        var k = height * width;

        var anchorCenters = GenerateAnchorCenters(inputSize, stride, 2);

        // this is >= in python but > here
        var pos_inds = IndicesOfElementsLargerThen(scores, thresh);
        if (pos_inds.size == 0)
        {
            return null;
        }

        anchorCenters = anchorCenters[pos_inds];
        bbox_preds = bbox_preds[pos_inds];
        scores = scores[pos_inds];

        var bboxes = Distance2Bbox(anchorCenters, bbox_preds);
        NDArray? kpss = null;

        if (kps_preds is not null)
        {
            kps_preds = kps_preds[pos_inds];
            kpss = Distance2Kps(anchorCenters, kps_preds);
            kpss = kpss.reshape(kpss.shape[0], -1, 2);
        }

        return (scores, bboxes, kpss);
    }

    /// <summary>
    /// Filter out duplicate detections (multiple boxes describing roughly the same area) using non max suppression.
    /// </summary>
    /// <param name="dets">All detections with their scores.</param>
    /// <param name="thresh">Non max suppression threshold.</param>
    /// <returns>Which detections to keep.</returns>
    private static List<int> NonMaxSupression(NDArray dets, float thresh)
    {
        var x1 = dets[":, 0"];
        var y1 = dets[":, 1"];
        var x2 = dets[":, 2"];
        var y2 = dets[":, 3"];
        var scores = dets[":, 4"];
        var areas = (x2 - x1 + 1) * (y2 - y1 + 1);

        // the clone should not be needed but NumSharp returns false values otherwise
        var order = scores.Clone().argsort<float>()["::-1"];

        List<int> keep = new();
        while (order.size > 0)
        {
            var i = order[0];
            keep.Add(i);

            if (order.size == 1)
            {
                break;
            }

            var xx1 = np.maximum(x1[i], x1[order["1:"]]);
            var yy1 = np.maximum(y1[i], y1[order["1:"]]);
            var xx2 = np.minimum(x2[i], x2[order["1:"]]);
            var yy2 = np.minimum(y2[i], y2[order["1:"]]);

            var w = np.maximum(0.0, xx2 - xx1 + 1);
            var h = np.maximum(0.0, yy2 - yy1 + 1);
            var inter = w * h;
            var ovr = inter / (areas[i] + areas[order["1:"]] - inter);

            // this is <= in python but < here
            var inds = IndicesOfElementsBelow(ovr, thresh).Clone();

            // NumSharp has a bug that throws an OutOfRangeException if we use the same NDArray on the
            // left and right side of a variable assignment.
            order = np.array(order, true)[inds + 1];
        }

        return keep;
    }

    private ModelParameters DetermineModelParameters()
    {
        var inputMeta = _session.InputMetadata;
        var inputName = inputMeta.Keys.First();
        var input = inputMeta[inputName];
        var (x, y) = (input.Dimensions[2], input.Dimensions[3]);
        var inputSize = x == -1 && y == -1 ? (Size?)null : new Size(x, y);

        var firstOutput = _session.OutputMetadata.Values.First();
        var batched = firstOutput.Dimensions.Length == 3;
        return new(inputSize, inputName, batched);
    }

    private readonly record struct ModelParameters(Size? InputSize, string InputName, bool Batching);
}

public record ScrfdDetectorOptions
{
    /// <summary>
    /// Gets the path to the onnx file that contains the model with dynamic input.
    /// </summary>
    public string ModelPath { get; init; } = default!;

    /// <summary>
    /// Resize the image to dimensions supported by the model if required. This detector throws an
    /// exception if this is set to false and an image is passed in unsupported dimensions.
    /// </summary>
    public bool AutoResizeInputToModelDimensions { get; init; } = true;

    public float NonMaxSupressionThreshold { get; init; } = 0.4f;

    public float ConfidenceThreshold { get; init; } = 0.5f;
}
