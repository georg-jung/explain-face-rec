// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using FaceAiSharp.Validation;

var rc = new RootCommand("FaceAiSharp validation tools");

var db = new Option<FileInfo>(
    name: "--db",
    description: "File to use as db to store results and for continuation",
    getDefaultValue: () => new FileInfo("faceaisharp-validation.litedb"));
rc.AddGlobalOption(db);

var dbEmbeddingCollectionName = new Option<string>(
    name: "---db-embedding-collection-name",
    getDefaultValue: () => "ArcfaceEmbeddings");

var dataset = new Option<DirectoryInfo>(
    name: "--dataset",
    getDefaultValue: () => new DirectoryInfo(@"C:\Users\georg\Downloads\lfw\lfw"));

var arcfaceModel = new Option<FileInfo>(
    name: "--arcface-model",
    getDefaultValue: () => new FileInfo(@"C:\Users\georg\facePics\arcfaceresnet100-8\resnet100\resnet100.onnx"));

var scrfdModel = new Option<FileInfo>(
    name: "--scrfd-model",
    getDefaultValue: () => new FileInfo(@"C:\Users\georg\OneDrive\Dokumente\ScrfdOnnx\scrfd_2.5g_bnkps.onnx"));

var generateEmbeddings = new Command("generate-embeddings") { dataset, arcfaceModel, scrfdModel };

#pragma warning disable SA1116 // Split parameters should start on line after declaration
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

generateEmbeddings.SetHandler(async (dataset, db, arcfaceModel, scrfdModel, dbEmbeddingCollectionName) =>
{
    using var cmd = new GenerateEmbeddings(dataset, db, arcfaceModel, scrfdModel, dbEmbeddingCollectionName);
    await cmd.Invoke();
}, dataset, db, arcfaceModel, scrfdModel, dbEmbeddingCollectionName);
rc.AddCommand(generateEmbeddings);

return await rc.InvokeAsync(args);
