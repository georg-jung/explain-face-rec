// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlazorFace.Extensions;

internal static class BlazorMarkupExtensions
{
    public static async Task SetImageStream(this IJSRuntime js, Image image, string imgId)
    {
        // async probably doesnt make a difference, given we're writing to a MemoryStream
        using var outStr = new MemoryStream();
        image.SaveAsJpeg(outStr);
        outStr.Position = 0;

        var dotnetImageStream = new DotNetStreamReference(outStr);
        await js.InvokeVoidAsync("setImage", imgId, dotnetImageStream);
    }

    public static async Task ClearImage(this IJSRuntime js, string imgId)
    {
        await js.InvokeVoidAsync("clearImage", imgId);
    }

    public static Stream? TryOpen(this InputFileChangeEventArgs e, long maxUploadSize, out bool uploadWasTooLarge)
    {
        try
        {
            var s = e.File.OpenReadStream(maxUploadSize);
            uploadWasTooLarge = false;
            return s;
        }
        catch (IOException ex)
          when (ex.Message.Contains("byte", StringComparison.OrdinalIgnoreCase))
        {
            uploadWasTooLarge = true;
            return null;
        }
    }

    public static IEnumerable<(Stream Content, string FileName)> TryOpenMultiple(this InputFileChangeEventArgs e, int maxFileCount, long maxUploadSize)
    {
        var files = e.GetMultipleFiles(maxFileCount);
        var skippedTooLargeFilesCount = 0;
        foreach (var file in files)
        {
            Stream? s = null;
            string? name = null;
            try
            {
                s = file.OpenReadStream(maxUploadSize);
                name = file.Name;
            }
            catch (IOException ex)
              when (ex.Message.Contains("byte", StringComparison.OrdinalIgnoreCase))
            {
                skippedTooLargeFilesCount++;
            }

            if (s is not null)
            {
                yield return (s, name!);
            }
        }
    }

    /// <summary>
    /// Tries to open the given <paramref name="stream"/> as image using ImageSharp.
    /// Catches any exceptions that are thrown while opening. Returns null if an
    /// exception was thrown.
    /// </summary>
    /// <param name="stream">The stream to read the image data from.</param>
    /// <returns>An image if the stream could be opened as image, null otherwise.</returns>
    public static async Task<Image<Rgb24>?> TryOpenImage(this Stream? stream)
    {
        try
        {
            return await Image.LoadAsync<Rgb24>(stream);
        }
        catch
        {
            return null;
        }
    }
}
