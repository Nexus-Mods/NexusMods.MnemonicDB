using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.EventSourcing.Events;
using NexusMods.EventSourcing.Serialization;

namespace NexusMods.EventSourcing;

public static class Services
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        return services
            .AddSingleton<EventSerializer>()
            .AddSingleton<ISerializer, UInt8Serializer>()
            .AddSingleton<ISerializer, UInt32Serializer>()
            .AddEvent<TransactionEvent>()
            .AddSingleton<IEntityContext, EntityContext>();
    }

}
