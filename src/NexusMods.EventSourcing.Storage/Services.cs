using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Serializers;

namespace NexusMods.EventSourcing.Storage;

public static class Services
{
    public static IServiceCollection AddEventSourcingStorage(this IServiceCollection services)
    {
        services.AddValueSerializer<UInt64Serializer>();
        services.AddValueSerializer<StringSerializer>();
        return services;
    }
}
