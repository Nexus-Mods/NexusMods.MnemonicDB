using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.EventSourcing.Events;
using NexusMods.EventSourcing.Serialization;

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
        return services
            .AddSingleton<ISerializer, EntityDefinitionSerializer>()
            .AddSingleton<ISerializationRegistry, SerializationRegistry>()
            .AddSingleton<ISerializer, GenericArraySerializer>()
            .AddSingleton<ISerializer, GenericEntityIdSerializer>()
            .AddSingleton<ISerializer, StringSerializer>()
            .AddSingleton<ISerializer, BoolSerializer>()
            .AddSingleton<BinaryEventSerializer>()
            .AddSingleton<ISerializer, UInt8Serializer>()
            .AddSingleton<ISerializer, UInt32Serializer>()
            .AddSingleton<ISerializer, UInt64Serializer>()
            .AddSingleton<ISerializer, EntityIdSerializer>()
            .AddEvent<TransactionEvent>()
            .AddSingleton<IEntityContext, EntityContext>();
    }

}
