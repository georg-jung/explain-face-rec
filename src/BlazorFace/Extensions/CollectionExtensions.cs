// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BlazorFace.Extensions;

internal static class CollectionExtensions
{
    // see https://stackoverflow.com/q/3705950/1200847
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> valueCreator)
    {
        return dict.TryGetValue(key, out var value) ? value : dict[key] = valueCreator();
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : new()
    {
        return dictionary.GetOrAdd(key, () => new TValue());
    }
}
