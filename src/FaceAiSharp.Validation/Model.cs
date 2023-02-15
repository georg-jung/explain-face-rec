// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using LiteDB;

namespace FaceAiSharp.Validation;

#pragma warning disable SA1649 // File name should match first type name

internal readonly record struct EmbedderResult(string Identity, string FilePath, float[] Embeddings);

internal record Embedding
{
    public ObjectId EmbeddingId { get; set; } = null!;

    public string Identity { get; set; } = null!;

    public int ImageNumber { get; set; } = default;

    public string FilePath { get; set; } = null!;

    public float[] Embeddings { get; set; } = null!;
}

internal record EmbeddingDistance()
{
    public ObjectId EmbeddingDistanceId { get; set; } = null!;

    public Embedding X { get; set; } = null!;

    public Embedding Y { get; set; } = null!;

    public float CosineDistance { get; set; }

    public float EuclideanDistance { get; set; }

    public bool SameIdentity { get; set; }
}
