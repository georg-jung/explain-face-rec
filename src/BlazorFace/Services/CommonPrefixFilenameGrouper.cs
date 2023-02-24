// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.RegularExpressions;
using Accord.Statistics.Filters;
using BlazorFace.Extensions;

namespace BlazorFace.Services;

public partial class CommonPrefixFilenameGrouper : IFilenameGrouper
{
    public IEnumerable<IGrouping<string?, string>> GroupFilenames(IReadOnlyCollection<string> filenames)
    {
        var regex = ParseFilename();
        var groups = filenames.GroupBy(x => regex.Match(Path.GetFileNameWithoutExtension(x)) switch
        {
            { Success: false } => null,
            Match m => m.Groups[1].Value,
        });

        var grouped = new Dictionary<string, List<string>>();
        var nonGrouped = new List<string>();
        foreach (var g in groups)
        {
            var lst = g.Key is null || g.Count() <= 1 ? nonGrouped : grouped.GetOrAdd(g.Key);
            lst.AddRange(g);
        }

        if (nonGrouped.Count > 0)
        {
            yield return new Grouping(null, nonGrouped);
        }

        foreach (var (k, v) in grouped)
        {
            yield return new Grouping(k, v);
        }
    }

    [GeneratedRegex("^(.{3,}?)[\\.\\-_ ]?(\\d{1,8})$")]
    private static partial Regex ParseFilename();

    private record Grouping(string? Key, IEnumerable<string> Values) : IGrouping<string?, string>
    {
        public IEnumerator<string> GetEnumerator() => Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
    }
}
