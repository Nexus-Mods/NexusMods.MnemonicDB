using System;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public static class ValueAdaptorExtensions
{
    /// <summary>
    /// Adds a simple value adaptor that uses a converter function to convert from a database type to a system type.
    /// a given .net type
    /// </summary>
    public static IServiceCollection AddValueAdaptor<TFrom, TTo>(this IServiceCollection services,
        Func<TFrom, TTo> converter)
        where TFrom : unmanaged
    {
        services.AddSingleton<IValueAdaptorFactory>(_ => new SimpleValueAdaptorFactory<TFrom, TTo>(converter));
        return services;
    }
}
