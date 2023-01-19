// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

namespace Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ImageSharp>();
    }
}
