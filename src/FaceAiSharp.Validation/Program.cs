// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics;
using System.Threading.Channels;
using FaceAiSharp;
using FaceAiSharp.Abstractions;
using FaceAiSharp.Extensions;
using FaceAiSharp.Validation;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

var sessOpts = new SessionOptions();
sessOpts.EnableMemoryPattern = false;
sessOpts.AppendExecutionProvider_DML();

var emb = new ArcFaceEmbeddingsGenerator(
    new()
    {
        ModelPath = @"C:\Users\georg\facePics\arcfaceresnet100-8\resnet100\resnet100.onnx",
    },
    sessOpts);

var opts = new MemoryCacheOptions();
var iopts = Options.Create(opts);
var cache = new MemoryCache(iopts);

var det = new ScrfdDetector(
    new()
    {
        ModelPath = @"C:\Users\georg\OneDrive\Dokumente\ScrfdOnnx\scrfd_2.5g_bnkps.onnx",
    },
    cache);

using var db = new LiteDatabase("embeddings.litedb");
var dbEmb = db.GetCollection<EmbedderResult>();
dbEmb.EnsureIndex(x => x.FilePath);

var setFolder = @"C:\Users\georg\Downloads\lfw\lfw";

var setEnum = DatasetIterator.EnumerateFolderPerIdentity(setFolder).Where(x => !dbEmb.Exists(db => db.FilePath == x.FilePath));
/* var embeddings = DatasetIterator.EnumerateEmbedderResults(emb, Preprocess, setEnum); */

var ch = Channel.CreateBounded<ChannelData>(10);
var producerTask = ProducePreprocessed(setEnum, ch);
var embeddings = GenerateEmbeddingsFromChannel(emb, ch.Reader);

await foreach (var (embRes, ticks) in embeddings)
{
    dbEmb.Insert(embRes);
    Console.WriteLine($"{ticks,4:D}ms : {embRes.FilePath}");
    db.Commit();
}

await producerTask;

Image<Rgb24> Preprocess(string filePath)
{
    var img = Image.Load<Rgb24>(filePath);
    var x = det.Detect(img);
    var first = x.First();
    Debug.Assert(first.Landmarks != null, "No landmarks detected but required");
    var angle = ScrfdDetector.GetFaceAlignmentAngle(first.Landmarks);
    img.CropAlignedDestructive(Rectangle.Round(first.Box), (float)angle);
    return img;
}

async Task ProducePreprocessed(IEnumerable<DatasetImage> images, ChannelWriter<ChannelData> channel)
{
    await Parallel.ForEachAsync(images, async (DatasetImage image, CancellationToken tok) =>
    {
        var sw = Stopwatch.StartNew();
        var img = Preprocess(image.FilePath);
        await channel.WriteAsync(new(image, img, sw.ElapsedMilliseconds));
    });

    channel.Complete();
}

static async IAsyncEnumerable<(EmbedderResult, long Ticks)> GenerateEmbeddingsFromChannel(IFaceEmbeddingsGenerator embedder, ChannelReader<ChannelData> channel)
{
    await foreach (var (metadata, img, ticks) in channel.ReadAllAsync())
    {
        var sw = Stopwatch.StartNew();
        var embedding = embedder.Generate(img);
        yield return (new(metadata.Identity, metadata.FilePath, embedding), ticks + sw.ElapsedMilliseconds);
    }
}

#pragma warning disable SA1649 // File name should match first type name
internal readonly record struct ChannelData(DatasetImage Metadata, Image<Rgb24> Image, long ElapsedTicks);
