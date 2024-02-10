using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

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
    public static IServiceCollection AddDatomStore(this IServiceCollection services)
    {
        services.AddSingleton<AttributeRegistry>()
            .AddAttribute<BuiltInAttributes.UniqueId>()
            .AddAttribute<BuiltInAttributes.ValueSerializerId>()
            .AddValueSerializer<UInt128Serializer>()
            .AddValueSerializer<BoolSerializer>();
        return services;
    }

}
