// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BlazorFace.Services;

public interface IFilenameGrouper
{
    IEnumerable<IGrouping<string?, string>> GroupFilenames(IReadOnlyCollection<string> filenames);
}
