// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace BlazorFace;

public static class Media
{
#if DEBUG
    public static readonly string MediaDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "wwwroot", "media");
#else
    public static readonly string MediaDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media");
#endif

    public static readonly string PortraitsDir = Path.Combine(MediaDir, "portraits");

    public static readonly string GroupsDir = Path.Combine(MediaDir, "groups");

    private static readonly Lazy<IReadOnlyList<string>> _portraits = new(() => Directory.GetFiles(PortraitsDir, "*.jpg"));

    private static readonly Lazy<IReadOnlyList<string>> _groups = new(() => Directory.GetFiles(GroupsDir, "*.jpg"));

    public static IReadOnlyList<string> Portraits => _portraits.Value;

    public static IReadOnlyList<string> Groups => _groups.Value;
}
