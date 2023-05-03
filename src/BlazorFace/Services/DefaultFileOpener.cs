// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace BlazorFace.Services
{
    public sealed class DefaultFileOpener : IFileOpener
    {
        public ValueTask<Stream> OpenAsync(string path)
        {
#if DEBUG
            var p = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, path);
#else
            var p = path;
#endif
            return ValueTask.FromResult<Stream>(File.OpenRead(p));
        }

        public byte[] ReadAllBytes(string path)
        {
#if DEBUG
            var p = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, path);
#else
            var p = path;
#endif
            return File.ReadAllBytes(p);
        }
    }
}
