// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using FaceAiSharp;
using FaceAiSharp.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NodaTime;

namespace BlazorFace
{
    public class Program
    {
        public static string? Version { get; private set; }

        public static void Main(string[] args)
        {
            Version = GetInformationalVersion();
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<IClock>(SystemClock.Instance);
            builder.Services.AddTransient<IFaceDetector, FaceOnnxDetector>();
            builder.Services.AddTransient<IFaceEmbeddingsGenerator, FaceOnnxEmbeddingsGenerator>();
            builder.Services.AddTransient<IFaceLandmarksExtractor, FaceOnnxLandmarkExtractor>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }

        private static string? GetInformationalVersion() =>
            Assembly
                .GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
    }
}
