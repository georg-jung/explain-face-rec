// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BlazorFace.Services
{
    public sealed class DefaultFileOpener : IFileOpener
    {
        public ValueTask<Stream> OpenAsync(string path) => ValueTask.FromResult<Stream>(File.OpenRead(path));
    }
}
