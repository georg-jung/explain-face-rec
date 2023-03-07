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
    public static readonly string BarackObama01 = Path.Combine(PortraitsDir, "Barack_Obama_01.jpg");
    public static readonly string JohnFKennedy01 = Path.Combine(PortraitsDir, "John_F_Kennedy_01.jpg");
}
