// Copyright (c) Georg Jung. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.ObjectPool;

namespace BlazorFace;

internal sealed class DIPooledObjectPolicy<T> : PooledObjectPolicy<T>
    where T : notnull
{
    private readonly IServiceProvider _serviceProvider;

    public DIPooledObjectPolicy(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override T Create() => _serviceProvider.GetRequiredService<T>();

    public override bool Return(T obj) => true;
}
