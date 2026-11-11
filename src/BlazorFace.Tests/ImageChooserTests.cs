// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BlazorFace.Services;
using BlazorFace.Shared;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlazorFace.Tests;

/// <summary>
/// The image choosers used to handle uploads in an <c>async void</c> method, so a failing
/// model left the spinner running forever and swallowed the exception. These tests pin down
/// that a failure is reported to the user and that the next upload still works.
/// </summary>
public sealed class ImageChooserTests
{
    private const string InternalError = "Internal model initialization details";

    private static readonly byte[] Jpeg = CreateJpeg();

    [Test]
    public async Task SingleChooserRecoversFromProcessingFailure()
    {
        using var context = CreateContext();
        var chooser = context.Render<SingleChooser>();
        chooser.Instance.Fail = true;

        await Upload(chooser);

        await Assert.That(chooser.Instance.Processing).IsFalse();
        await Assert.That(chooser.Find(".invalid-feedback.d-block").TextContent).Contains("could not be processed");
        await Assert.That(chooser.Markup).DoesNotContain(InternalError);

        chooser.Instance.Fail = false;
        await Upload(chooser);

        await Assert.That(chooser.Instance.Calls).IsEqualTo(2);
        await Assert.That(chooser.FindAll(".invalid-feedback")).IsEmpty();
    }

    [Test]
    public async Task GroupChooserRecoversFromProcessingFailure()
    {
        using var context = CreateContext();
        var chooser = context.Render<GroupChooser>();
        chooser.Instance.Fail = true;

        await Upload(chooser);

        await Assert.That(chooser.Instance.Processing).IsFalse();
        await Assert.That(chooser.Markup).Contains("could not all be processed").And.DoesNotContain(InternalError);
        await Assert.That(chooser.FindAll(".progress")).IsEmpty();

        chooser.Instance.Fail = false;
        await Upload(chooser);

        await Assert.That(chooser.Instance.Calls).IsEqualTo(2);
        await Assert.That(chooser.Markup).DoesNotContain("could not all be processed");
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddLogging();
        context.Services.AddSingleton<IFileOpener>(new ImageOpener());
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static Task Upload<T>(IRenderedComponent<T> chooser)
        where T : class, IComponent
        => chooser.InvokeAsync(() => chooser.FindComponent<InputFile>().Instance.OnChange
            .InvokeAsync(new InputFileChangeEventArgs([new ImageFile()])));

    private static byte[] CreateJpeg()
    {
        using var image = new Image<Rgb24>(2, 2);
        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    public sealed class SingleChooser : ChooseSingleImageBase
    {
        public bool Fail { get; set; }

        public int Calls { get; private set; }

        public bool Processing => IsProcessing;

        protected override Task<string?> OnImageLoadedAsync(Image<Rgb24> image)
        {
            Calls++;
            return Fail ? throw new InvalidOperationException(InternalError) : Task.FromResult<string?>(null);
        }
    }

    public sealed class GroupChooser : ChooseImageGroupBase
    {
        public bool Fail { get; set; }

        public int Calls { get; private set; }

        public bool Processing => IsProcessing;

        protected override Task<string?> OnImageLoadedAsync(Image<Rgb24> image, string fileName)
        {
            Calls++;
            return Fail ? throw new InvalidOperationException(InternalError) : Task.FromResult<string?>(null);
        }
    }

    private sealed class ImageFile : IBrowserFile
    {
        public string Name => "face.jpg";

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => Jpeg.Length;

        public string ContentType => "image/jpeg";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(Jpeg, writable: false);
    }

    private sealed class ImageOpener : IFileOpener
    {
        public ValueTask<Stream> OpenAsync(string path)
            => ValueTask.FromResult<Stream>(new MemoryStream(Jpeg, writable: false));

        public byte[] ReadAllBytes(string path) => Jpeg;
    }
}
