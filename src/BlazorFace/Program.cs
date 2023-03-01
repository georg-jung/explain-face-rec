// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using BlazorFace.Services;
using FaceAiSharp;
using FaceAiSharp.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
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

            ConfigureBlazorFaceServices(builder);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            AddBlazorFaceServices(builder.Services);

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
                        Path.Combine(builder.Environment.ContentRootPath, @"..\..\media")),
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

        public static void ConfigureBlazorFaceServices(IServiceCollection services, IConfiguration configuration)
        {
            ConfigureOptionsIndependent<ArcFaceEmbeddingsGeneratorOptions>(services, configuration);
            ConfigureOptionsIndependent<ScrfdDetectorOptions>(services, configuration);
            ConfigureOptionsIndependent<OpenVinoOpenClosedEye0001Options>(services, configuration);
        }

        public static void AddBlazorFaceServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IClock>(SystemClock.Instance);
            services.AddSingleton<IFilenameGrouper, CommonPrefixFilenameGrouper>();
            services.AddTransient<IFaceDetector, ScrfdDetector>();
            services.AddTransient<IFaceEmbeddingsGenerator, ArcFaceEmbeddingsGenerator>();
            services.AddTransient<IFaceLandmarksExtractor, FaceOnnxLandmarkExtractor>();
            services.AddTransient<IEyeStateDetector, OpenVinoOpenClosedEye0001>();

            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            AddInjectionObjectPool<IFaceDetector>(services);
            AddInjectionObjectPool<IFaceEmbeddingsGenerator>(services);
            AddInjectionObjectPool<IFaceLandmarksExtractor>(services);
            AddInjectionObjectPool<IEyeStateDetector>(services);
        }

        private static void ConfigureBlazorFaceServices(WebApplicationBuilder builder)
            => ConfigureBlazorFaceServices(builder.Services, builder.Configuration);

        /// <summary>
        /// Similar to Microsoft's
        /// <see cref="OptionsServiceCollectionExtensions.Configure{TOptions}(IServiceCollection, Action{TOptions})"/>
        /// method but does not only register
        /// <see cref="IOptions{TOptions}"/>
        /// in DI but also adds <typeparamref name="TOptions"/> directly as a singleton.
        /// Useful for configuring services that use the concept of an options type but
        /// don't depend on IOptions for implementation.
        /// </summary>
        private static void ConfigureOptionsIndependent<TOptions>(IServiceCollection services, IConfiguration configuration)
            where TOptions : class, new()
        {
            const string Options = "Options";
            var name = typeof(TOptions).Name;
            if (name.EndsWith(Options, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - Options.Length);
            }

            // see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-7.0#validateonstart
            services.AddOptions<TOptions>()
                .Bind(configuration.GetSection(name))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<TOptions>>().Value);
        }

        private static void AddInjectionObjectPool<T>(IServiceCollection serviceCollection)
            where T : class
        {
            serviceCollection.AddSingleton(serviceProvider =>
            {
                var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
                var pol = new DIPooledObjectPolicy<T>(serviceProvider);
                return provider.Create(pol);
            });
        }

        private static string? GetInformationalVersion() =>
            Assembly
                .GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
    }
}
