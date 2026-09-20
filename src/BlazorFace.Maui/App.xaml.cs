// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BlazorFace.Maui;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Resolve after application resources are initialized, with a fresh page for each window.
        return new Window(_services.GetRequiredService<MainPage>())
        {
            Title = "Understanding Face Recognition",
        };
    }
}
