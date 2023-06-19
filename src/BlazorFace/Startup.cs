// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Blazored.Modal;
using BlazorFace.Services;
using FaceAiSharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using NodaTime;

namespace BlazorFace;

public static class Startup
{
    public static bool ShowTryLocallySection { get; set; } = false;

    private static readonly Lazy<string?> _version = new(GetInformationalVersion);

    public static string? Version => _version.Value;

    public static void ConfigureBlazorFaceServices(IServiceCollection services, IConfiguration configuration)
    {
        ConfigureOptionsIndependent<ArcFaceEmbeddingsGeneratorOptions>(services, configuration);
        ConfigureOptionsIndependent<ScrfdDetectorOptions>(services, configuration);
        ConfigureOptionsIndependent<OpenVinoOpenClosedEye0001Options>(services, configuration);
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        services.PostConfigure<ArcFaceEmbeddingsGeneratorOptions>(opt => opt.ModelPath ??= Path.Combine(exeDir, @"onnx/arcfaceresnet100-11-int8.onnx"));
        services.PostConfigure<ScrfdDetectorOptions>(opt => opt.ModelPath ??= Path.Combine(exeDir, @"onnx/scrfd_2.5g_kps.onnx"));
        services.PostConfigure<OpenVinoOpenClosedEye0001Options>(opt => opt.ModelPath ??= Path.Combine(exeDir, @"onnx/open_closed_eye.onnx"));
    }

    public static void AddBlazorFaceServices(IServiceCollection services, IFileOpener? onnxModelFileOpener = null)
    {
        services.AddBlazoredModal();

        services.AddMemoryCache();
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddSingleton<IFilenameGrouper, CommonPrefixFilenameGrouper>();
        if (onnxModelFileOpener is null)
        {
            services.AddTransient<IFaceDetectorWithLandmarks, ScrfdDetector>();
            services.AddTransient<IFaceEmbeddingsGenerator, ArcFaceEmbeddingsGenerator>();
            services.AddTransient<IEyeStateDetector, OpenVinoOpenClosedEye0001>();
        }
        else
        {
            services.AddTransient<IFaceDetector, ScrfdDetector>(sp => new ScrfdDetector(onnxModelFileOpener.ReadAllBytes(@"onnx/scrfd_2.5g_kps.onnx"), sp.GetRequiredService<IMemoryCache>(), sp.GetRequiredService<ScrfdDetectorOptions>()));
            services.AddTransient<IFaceEmbeddingsGenerator, ArcFaceEmbeddingsGenerator>(sp => new ArcFaceEmbeddingsGenerator(onnxModelFileOpener.ReadAllBytes(@"onnx/arcfaceresnet100-11-int8.onnx"), sp.GetRequiredService<ArcFaceEmbeddingsGeneratorOptions>()));
            services.AddTransient<IEyeStateDetector, OpenVinoOpenClosedEye0001>(sp => new OpenVinoOpenClosedEye0001(onnxModelFileOpener.ReadAllBytes(@"onnx/open_closed_eye.onnx"), sp.GetRequiredService<OpenVinoOpenClosedEye0001Options>()));
        }

        services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>(sp => new DefaultObjectPoolProvider
        {
            MaximumRetained = 1,
        });
        AddInjectionObjectPool<IFaceDetectorWithLandmarks>(services);
        AddInjectionObjectPool<IFaceEmbeddingsGenerator>(services);
        AddInjectionObjectPool<IEyeStateDetector>(services);
    }

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
        ThisAssembly.AssemblyInformationalVersion;
}
