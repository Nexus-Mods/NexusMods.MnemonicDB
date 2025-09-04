using System;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.HyperDuck.BindingConverters;

public static class BindingConverterExtensions
{
    /// <summary>
    /// Adds a simple binding converter that uses a converter function to convert from a system type to a database type.
    /// </summary>
    public static IServiceCollection AddBindingConverter<TFrom, TTo>(this IServiceCollection services, Func<TFrom, TTo> func)
    {
        services.AddSingleton<IBindingConverter>(new SimpleBindingConverter<TFrom, TTo>(func));
        return services;
    }
    
}
