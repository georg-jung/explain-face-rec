// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using FaceAiSharp.Abstractions;
using FaceAiSharp.Extensions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FaceAiSharp;

public class OpenVinoOpenClosedEye0001 : IEyeStateDetector, IDisposable
{
    private static readonly ResizeOptions _resizeOptions = new()
    {
        Mode = ResizeMode.Pad,
        PadColor = Color.Black,
        Size = new Size(32, 32),
    };

    private readonly InferenceSession _session;

    public OpenVinoOpenClosedEye0001(OpenVinoOpenClosedEye0001Options options, SessionOptions? sessionOptions = null)
    {
        Options = options;
        if (sessionOptions is null)
        {
            _session = new(options.ModelPath);
        }
        else
        {
            _session = new(options.ModelPath, sessionOptions);
        }
    }

    public OpenVinoOpenClosedEye0001Options Options { get; }

    public void Dispose() => _session.Dispose();

    public bool IsOpen(Image<Rgb24> eyeImage)
    {
        eyeImage.EnsureProperlySizedDestructive(_resizeOptions, !Options.AutoResizeInputToModelDimensions);

        var input = CreateImageTensor(eyeImage);

        var inputMeta = _session.InputMetadata;
        var name = inputMeta.Keys.First();

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, input) };
        using var outputs = _session.Run(inputs);
        var firstOut = outputs.First();
        var tens = firstOut.Value as DenseTensor<float> ?? firstOut.AsTensor<float>().ToDenseTensor();
        Debug.Assert(tens.Length % 2 == 0, "Output tensor length is invalid.");

        var span = tens.Buffer.Span;
        return span[0] < span[1];
    }

    internal static DenseTensor<float> CreateImageTensor(Image<Rgb24> img)
    {
        // The model uses the bgr values, the ints converted to float, no further preprocessing needed.
        var mean = new[] { 0.5f, 0.5f, 0.5f };
        var stdDevVal = 1f;
        var stdDev = new[] { stdDevVal, stdDevVal, stdDevVal };
        var inputDim = new[] { 1, 3, 32, 32 };
        return img.ToTensor(mean, stdDev, inputDim, true);
    }
}

public record OpenVinoOpenClosedEye0001Options
{
    /// <summary>
    /// Gets the path to the onnx file that contains open-closed-eye-0001/open_closed_eye.onnx with 1x3x32x32 BGR input.
    /// </summary>
    public string ModelPath { get; init; } = default!;

    /// <summary>
    /// Resize the image to dimensions supported by the model if required. This detector throws an
    /// exception if this is set to false and an image is passed in unsupported dimensions.
    /// </summary>
    public bool AutoResizeInputToModelDimensions { get; init; } = true;
}
