// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Blazored.Modal;
using BlazorFace.Services;
using Microsoft.Extensions.FileProviders;

namespace BlazorFace.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureBlazorFaceServices(builder);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            builder.Services.AddSingleton<IFileOpener, DefaultFileOpener>();

            BlazorFace.Startup.ShowTryLocallySection = true;
            BlazorFace.Startup.AddBlazorFaceServices(builder.Services);

            // Add the following line:
            builder.WebHost.UseSentry(o =>
            {
                o.TracesSampleRate = 1.0;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (app.Environment.IsDevelopment())
            {
                // this is required because the media folder that is just linked into the wwwroot
                // will not be copied in debug builds.
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(
                        Path.Combine(builder.Environment.ContentRootPath, @"../../media")),
                    RequestPath = "/media",
                });
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseSentryTracing();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }

        public static void ConfigureBlazorFaceServices(WebApplicationBuilder builder)
            => BlazorFace.Startup.ConfigureBlazorFaceServices(builder.Services, builder.Configuration);
    }
}
