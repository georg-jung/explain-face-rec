// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace BlazorFace;

public static class Media
{
#if DEBUG
    public static readonly string PortraitsDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "wwwroot", "media", "portraits");
#else
    public static readonly string PortraitsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media", "portraits");
#endif

    private static readonly Lazy<IReadOnlyList<string>> _portraits = new(() => Directory.GetFiles(PortraitsDir, "*.jpg"));

    public static IReadOnlyList<string> Portraits => _portraits.Value;
}
