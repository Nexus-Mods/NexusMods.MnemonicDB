using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Extension methods for adding attributes and other types to the service collection.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Registers the event sourcing services with the service collection.
    /// </summary>
    public static IServiceCollection AddMnemonicDB(this IServiceCollection services)
    {
        services.AddSingleton<IConnection, Connection>();
        services.AddHostedService<Connection>(s => (Connection)s.GetRequiredService<IConnection>());
        return services;
    }
}
