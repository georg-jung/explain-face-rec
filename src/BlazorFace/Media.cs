// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using DirectoryListingSourceGenerator;
using Markdig;

namespace BlazorFace;

public static partial class Media
{
#if ANDROID
    public static readonly string MediaDir = "wwwroot/media";
#elif DEBUG
    public static readonly string MediaDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "wwwroot", "media");
#else
    public static readonly string MediaDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media");
#endif

    public static readonly string PortraitsDir = Path.Combine(MediaDir, "portraits");

    public static readonly string GroupsDir = Path.Combine(MediaDir, "groups");

    public static readonly string LfwDir = Path.Combine(MediaDir, "lfw");

    private static readonly Lazy<string> _sourcesHtml = new(() => Markdown.ToHtml(StaticResources.MediaSourcesMd));

    private static readonly Lazy<IReadOnlyList<string>> _portraits = new(() => new List<string>(PortraitsListing().Select(x => Path.Combine(PortraitsDir, x))));

    private static readonly Lazy<IReadOnlyList<string>> _groups = new(() => new List<string>(GroupsListing().Select(x => Path.Combine(PortraitsDir, x))));

    private static readonly Lazy<IReadOnlyDictionary<string, string[]>> _lfwFaces = new(() => LfwFacesListing().ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(x => Path.Combine(LfwDir, kvp.Key, x)).ToArray()));

    public static string SourcesHtml => _sourcesHtml.Value;

    public static IReadOnlyList<string> Portraits => _portraits.Value;

    public static IReadOnlyList<string> Groups => _groups.Value;

    public static IReadOnlyDictionary<string, string[]> LfwFaces => _lfwFaces.Value;

    [DirectoryListing(@"../../media/portraits")]
    private static partial string[] PortraitsListing();

    [DirectoryListing(@"../../media/groups")]
    private static partial string[] GroupsListing();

    [DirectoryListing(@"../../media/lfw")]
    private static partial IReadOnlyDictionary<string, string[]> LfwFacesListing();
}
