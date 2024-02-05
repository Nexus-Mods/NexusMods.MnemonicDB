using DuckDB.NET;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
/// The services for the Event Sourcing library.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the Event Sourcing services to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        services.AddSingleton<IDatomStore, DuckDBDatomStore>();
        services.AddSingleton<IConnection, Connection>();
        return services;
    }

    private static IServiceCollection AddDefaultAttributes(this IServiceCollection services)
    {
        return services;
    }

}
