// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading.Channels;
using FaceAiSharp.Abstractions;
using SimpleSimd;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceAiSharp.Validation;

internal class DatasetIterator
{
    public static IEnumerable<DatasetImage> EnumerateFolderPerIdentity(string parent, string searchPattern = "*.jpg")
    {
        var withoutSlash = Path.GetFullPath(parent);
        var cutoff = withoutSlash.Length + 1; // len with slash
        foreach (var file in Directory.EnumerateFiles(parent, searchPattern, SearchOption.AllDirectories))
        {
            var id = Path.GetDirectoryName(file)!.Substring(cutoff);
            yield return new DatasetImage(id, file);
        }
    }

    public static IEnumerable<EmbedderResult> EnumerateEmbedderResults(IFaceEmbeddingsGenerator embedder, Func<string, Image<Rgb24>> fPreprocess, IEnumerable<DatasetImage> input)
    {
        foreach (var batch in input.Buffer(32))
        {
            var entries = new ConcurrentBag<(DatasetImage Entry, Image<Rgb24> Image)>();
            Parallel.ForEach(batch, x =>
            {
                var img = fPreprocess(x.FilePath);
                entries.Add((x, img));
            });

            var embs = entries.Select(x => embedder.Generate(x.Image));
            foreach (var (embedding, (entry, img)) in embs.Zip(entries))
            {
                img.Dispose();
                yield return new(entry.Identity, entry.FilePath, embedding);
            }
        }
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "I like it here")]
internal readonly record struct DatasetImage(string Identity, string FilePath);

internal readonly record struct EmbedderResult(string Identity, string FilePath, float[] Embeddings);
