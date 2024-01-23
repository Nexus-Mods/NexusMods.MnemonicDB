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
            .AddSingleton<ISerializer, GuidSerializer>()
            .AddSingleton<ISerializer, Int16Serializer>()
            .AddSingleton<ISerializer, Int32Serializer>()
            .AddSingleton<ISerializer, Int64Serializer>()
            .AddSingleton<ISerializer, UInt8Serializer>()
            .AddSingleton<ISerializer, UInt16Serializer>()
            .AddSingleton<ISerializer, UInt32Serializer>()
            .AddSingleton<ISerializer, UInt64Serializer>()
            .AddSingleton<ISerializer, FloatSerializer>()
            .AddSingleton<ISerializer, DoubleSerializer>()
            .AddSingleton<ISerializer, EntityIdSerializer>()
            .AddSingleton<BinaryEventSerializer>()
            .AddEvent<TransactionEvent>()
            .AddSingleton<IEntityContext, EntityContext>();
    }

}
