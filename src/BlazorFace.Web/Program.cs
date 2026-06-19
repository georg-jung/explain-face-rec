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
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddSingleton<IFileOpener, DefaultFileOpener>();

            BlazorFace.Startup.ShowTryLocallySection = true;
            BlazorFace.Startup.AddBlazorFaceServices(builder.Services);

            var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? Environment.GetEnvironmentVariable("SENTRY_DSN");
            var sentryEnabled = !string.IsNullOrEmpty(sentryDsn);

            if (sentryEnabled)
            {
                builder.WebHost.UseSentry(o =>
                {
                    o.TracesSampleRate = 1.0;
                });
            }

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            if (sentryEnabled)
            {
                app.UseSentryTracing();
            }
            else
            {
                app.Logger.LogInformation("Sentry DSN is not configured. Sentry integration is disabled.");
            }

            app.MapStaticAssets();
            app.MapRazorComponents<Components.App>()
                .AddInteractiveServerRenderMode()
                .AddAdditionalAssemblies(typeof(BlazorFace.Components.Routes).Assembly);

            app.Run();
        }

        public static void ConfigureBlazorFaceServices(WebApplicationBuilder builder)
            => BlazorFace.Startup.ConfigureBlazorFaceServices(builder.Services, builder.Configuration);
    }
}
