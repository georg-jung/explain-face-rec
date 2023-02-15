// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FaceAiSharp.Validation;

internal class DatasetIterator
{
    public static IEnumerable<DatasetImage> EnumerateFolderPerIdentity(string parent, string searchPattern = "*.jpg")
    {
        var withoutSlash = Path.GetFullPath(parent);
        var cutoff = withoutSlash.Length + 1; // len with slash
        foreach (var file in Directory.EnumerateFiles(parent, searchPattern, SearchOption.AllDirectories))
        {
            var id = Path.GetDirectoryName(file)!.Substring(cutoff); // eg. John_Doe
            var fileNameOnly = Path.GetFileNameWithoutExtension(file)!; // eg. John_Doe_0001
            var imgNumStr = fileNameOnly.Substring(id.Length + 1); // eg. 0001
            var imgNum = Convert.ToInt32(imgNumStr);
            yield return new DatasetImage(id, imgNum, file);
        }
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "I like it here")]
internal readonly record struct DatasetImage(string Identity, int ImageNumber, string FilePath);
