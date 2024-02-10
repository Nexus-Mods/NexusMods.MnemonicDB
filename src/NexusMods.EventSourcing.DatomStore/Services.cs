using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore;

/// <summary>
/// Extension methods for adding attributes and other types to the service collection.
/// </summary>
public static class Services
{
    /// <summary>
    /// Registers the event sourcing services with the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddEventSourcing<TAttribute>(this IServiceCollection services)
        where TAttribute : class, IAttribute
    {
        services.AddSingleton<AttributeRegistry>();
        return services;
    }

}
