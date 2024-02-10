using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Extension methods for adding attributes and other types to the service collection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the specified attribute type with the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAttribute<TAttribute>(this IServiceCollection services)
        where TAttribute : class, IAttribute
    {
        services.AddSingleton<IAttribute, TAttribute>();
        return services;
    }


    /// <summary>
    /// Adds the value serializer to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TValueSerializer"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddValueSerializer<TValueSerializer>(this IServiceCollection services)
        where TValueSerializer : class, IValueSerializer
    {
        services.AddSingleton<IValueSerializer, TValueSerializer>();
        return services;
    }
}
