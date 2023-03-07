// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlazorFace.Extensions;

internal static class BlazorMarkupExtensions
{
    public static async Task SetImageStream(this IJSRuntime js, byte[] image, string imgId)
    {
        using var ms = new MemoryStream(image);
        var dotnetImageStream = new DotNetStreamReference(ms);
        await js.InvokeVoidAsync("setImage", imgId, dotnetImageStream);
    }

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
        => TryOpen(e.File, maxUploadSize, out uploadWasTooLarge);

    public static Stream? TryOpen(this IBrowserFile bf, long maxUploadSize, out bool uploadWasTooLarge)
    {
        try
        {
            var s = bf.OpenReadStream(maxUploadSize);
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

    public static IEnumerable<(Stream? Content, string FileName, bool TooLarge)> TryOpenMultiple(this InputFileChangeEventArgs e, int maxFileCount, long maxUploadSize)
    {
        var files = e.GetMultipleFiles(maxFileCount);
        foreach (var file in files)
        {
            Stream? s = null;
            string? name = null;
            try
            {
                name = file.Name;
                s = file.OpenReadStream(maxUploadSize);
            }
            catch (IOException ex)
              when (ex.Message.Contains("byte", StringComparison.OrdinalIgnoreCase))
            {
            }

            yield return (s, name!, s is null);
        }
    }

    public static async Task<Image<Rgb24>?> TryOpenImageBrowserConverted(this IBrowserFile file, int maxNoConversionSize, long maxUploadSize, int maxWidth, int maxHeight, ILogger? log)
    {
        const string jpegMime = "image/jpeg";
        async Task<IBrowserFile> Converted() => await file.RequestImageFileAsync(jpegMime, maxWidth, maxHeight);

        var bf = file;
        if (bf.Size > maxNoConversionSize)
        {
            bf = await Converted();
            log?.LogInformation($"Converted {file.Size} byte {file.ContentType} to {bf.Size} byte {bf.ContentType} in the user's browser.");
            if (bf.Size > file.Size)
            {
                bf = file;
                log?.LogWarning($"Using the original file because it is smaller.");
            }
        }

        var s = bf.TryOpen(maxUploadSize, out var _);
        return s is null ? null : await s.TryOpenImage();
    }

    /// <summary>
    /// Tries to open the given <paramref name="stream"/> as image using ImageSharp.
    /// Catches any exceptions that are thrown while opening. Returns null if an
    /// exception was thrown.
    /// </summary>
    /// <param name="stream">The stream to read the image data from.</param>
    /// <returns>An image if the stream could be opened as image, null otherwise.</returns>
    public static async Task<Image<Rgb24>?> TryOpenImage(this Stream stream)
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
