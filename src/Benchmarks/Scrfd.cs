// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using BenchmarkDotNet.Attributes;
using FaceAiSharp;
using FaceAiSharp.Abstractions;
using FaceAiSharp.Extensions;
using FaceONNX;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Benchmarks;

[MemoryDiagnoser]
public class Scrfd
{
    private readonly Image _img = Image.Load(@"C:\Users\georg\facePics\avGroup.jpg");
    private readonly Image<Rgb24> _preprocImg;
    private readonly Image<RgbaVector> _preprocImg2;
    private readonly ScrfdDetector _scrfd1;
    private readonly ScrfdDetector _scrfd2;
    private readonly DenseTensor<float> _imgTensor;

    public Scrfd()
    {
        var x = _img.EnsureProperlySized<Rgb24>(
            new ResizeOptions()
            {
                Size = new Size(640),
                Position = AnchorPositionMode.TopLeft,
                Mode = ResizeMode.BoxPad,
                PadColor = Color.Black,
            },
            false);
        var x2 = x.Image.CloneAs<RgbaVector>();

        _preprocImg = x.Image;
        _preprocImg2 = x2;
        var opts = new MemoryCacheOptions();
        var iopts = Options.Create(opts);
        var c1 = new MemoryCache(iopts);
        var c2 = new MemoryCache(iopts);
        _imgTensor = ScrfdDetector.CreateImageTensor(_preprocImg);
        _scrfd1 = new(
            new()
            {
                ModelPath = @"C:\Users\georg\OneDrive\Dokumente\ScrfdOnnx\scrfd_2.5g_bnkps_shape640x640.onnx",
                AutoResizeInputToModelDimensions = false,
            },
            c1);

        _scrfd2 = new(
            new()
            {
                ModelPath = @"C:\Users\georg\OneDrive\Dokumente\ScrfdOnnx\scrfd_2.5g_bnkps_dyn.onnx",
                AutoResizeInputToModelDimensions = false,
            },
            c2,
            new()
            {
                ExecutionMode = Microsoft.ML.OnnxRuntime.ExecutionMode.ORT_PARALLEL,
            });
    }

    /*
    [Benchmark]
    public IReadOnlyCollection<FaceDetectorResult> First() => _scrfd1.Detect(_imgTensor, new Size(640, 640), 1.0f);

    [Benchmark]
    public IReadOnlyCollection<FaceDetectorResult> Second() => _scrfd2.Detect(_imgTensor, new Size(640, 640), 1.0f);
    */

    [Benchmark(Baseline = true)]
    public DenseTensor<float> ProductionImpl() => ScrfdDetector.CreateImageTensor(_preprocImg);

    [Benchmark]
    public DenseTensor<float> Vector3() => Vector3(_preprocImg);

    [Benchmark]
    public DenseTensor<float> ProcessPixelRowsAsVector4() => ProcessPixelRowsAsVector4(_preprocImg);

    [Benchmark]
    public DenseTensor<float> ProcessPixelRowsAsVector4RgbaVector() => ProcessPixelRowsAsVector4RgbaVector(_preprocImg2);

    [Benchmark]
    public DenseTensor<float> OptimizedBySkywalkerisnull() => OptimizedBySkywalkerisnull(_preprocImg);

    private static DenseTensor<float> Vector3(Image<Rgb24> img)
    {
        var ret = new DenseTensor<float>(new[] { 1, 3, img.Height, img.Width });

        var mean = new Vector3(0.5f);
        var max = new Vector3(byte.MaxValue);

        img.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgb24> pixelSpan = accessor.GetRowSpan(y);

                // pixelSpan.Length allows optimizations over accessor.Width but is semantically equivalent
                // see https://docs.sixlabors.com/articles/imagesharp/pixelbuffers.html
                for (var x = 0; x < pixelSpan.Length; x++)
                {
                    ref var val = ref pixelSpan[x];
                    var pxVec = new Vector3(val.R, val.G, val.B);
                    pxVec = (pxVec / max) - mean;
                    ret[0, 0, y, x] = pxVec.X;
                    ret[0, 1, y, x] = pxVec.Y;
                    ret[0, 2, y, x] = pxVec.Z;
                }
            }
        });

        return ret;
    }

    private static DenseTensor<float> ProcessPixelRowsAsVector4(Image<Rgb24> img)
    {
        var ret = new DenseTensor<float>(new[] { 1, 3, img.Height, img.Width });

        var mean = new Vector4(0.5f);
        var max = new Vector4(byte.MaxValue);

        img.Mutate(op => op.ProcessPixelRowsAsVector4((row, z) =>
            {
                for (int x = 0; x < row.Length; x++)
                {
                    var y = z.Y;
                    var pxVec = row[x] - mean;
                    ret[0, 0, y, x] = pxVec.X;
                    ret[0, 1, y, x] = pxVec.Y;
                    ret[0, 2, y, x] = pxVec.Z;
                }
            }));

        return ret;
    }

    private static DenseTensor<float> ProcessPixelRowsAsVector4RgbaVector(Image<RgbaVector> img)
    {
        var ret = new DenseTensor<float>(new[] { 1, 3, img.Height, img.Width });

        var mean = new Vector4(0.5f);
        var max = new Vector4(byte.MaxValue);

        img.Mutate(op => op.ProcessPixelRowsAsVector4((row, z) =>
        {
            for (int x = 0; x < row.Length; x++)
            {
                var y = z.Y;
                var pxVec = row[x] - mean;
                ret[0, 0, y, x] = pxVec.X;
                ret[0, 1, y, x] = pxVec.Y;
                ret[0, 2, y, x] = pxVec.Z;
            }
        }));

        return ret;
    }

    private static DenseTensor<float> OptimizedBySkywalkerisnull(Image<Rgb24> img)
    {
        var mean = new[] { 0.5f, 0.5f, 0.5f };
        var stddev = new[] { 1f, 1f, 1f };
        var dims = new[] { 1, 3, 640, 640 };
        return img.ToTensor(mean, stddev, dims);
    }
}
