// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BlazorFace.Services;

namespace BlazorFace.Maui
{
    internal class MauiResourceOpener : IFileOpener
    {
        public async ValueTask<Stream> OpenAsync(string path) => await FileSystem.Current.OpenAppPackageFileAsync(path);

        public byte[] ReadAllBytes(string path)
        {
            using var s = OpenAsync(path).GetAwaiter().GetResult();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
