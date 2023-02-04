// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;

namespace FaceAiSharp.Extensions;

public static class TensorExtensions
{
    public static NDArray ToNDArray<T>(this NamedOnnxValue input)
    {
        var denseTensor = input as DenseTensor<T> ?? input.AsTensor<T>().ToDenseTensor();
        var arr = new NDArray(typeof(T), new Shape(denseTensor.Dimensions.ToArray(), denseTensor.Strides.ToArray()));

        // todo: is it possible to avoid copying here?
        arr.SetData(denseTensor.ToArray());
        return arr;
    }
}
