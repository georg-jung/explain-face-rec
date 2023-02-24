// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BlazorFace.Helper;

internal static class ColorEnumerator
{
    public static IEnumerable<string> Plotly(bool endlessSequence)
    {
        // taken from https://stackoverflow.com/a/63460218/1200847
        do
        {
            yield return "#636EF0";
            yield return "#EF5530";
            yield return "#00CC90";
            yield return "#AB63F0";
            yield return "#FFA150";
            yield return "#19D3F0";
            yield return "#FF6690";
            yield return "#B6E880";
            yield return "#FF97F0";
            yield return "#FECB50";
        }
        while (endlessSequence);
    }

    public static IEnumerable<string> RepeatForever(string value)
    {
        while (true)
        {
            yield return value;
        }
    }
}
