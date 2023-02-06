// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;

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
}
