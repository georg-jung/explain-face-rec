// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DirectoryListingSourceGenerator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class DirectoryListingAttribute : Attribute
{
    public DirectoryListingAttribute(string path)
    {
        Path = path;
    }

    public string Path { get; }
}
