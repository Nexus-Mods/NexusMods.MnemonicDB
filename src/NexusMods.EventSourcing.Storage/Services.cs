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
        services.AddValueSerializer<SymbolSerializer>();
        services.AddValueSerializer<TxIdSerializer>();
        services.AddValueSerializer<EntityIdSerializer>();
        services.AddAttribute<BuiltInAttributes.UniqueId>();
        services.AddAttribute<BuiltInAttributes.ValueSerializerId>();
        services.AddSingleton<DatomStoreSettings>();
        services.AddSingleton<IDatomStore, DatomStore>();
        services.AddSingleton<NodeStore>();
        return services;
    }
}
