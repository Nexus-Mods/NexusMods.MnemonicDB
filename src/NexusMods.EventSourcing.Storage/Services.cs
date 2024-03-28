using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.RocksDbBackend;
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
        services.AddSingleton<IDatomStore, DatomStore>();
        services.AddSingleton<AttributeRegistry>();
        return services;
    }

    public static IServiceCollection AddDatomStoreSettings(this IServiceCollection services,
        DatomStoreSettings settings)
    {
        services.AddSingleton(settings);
        return services;
    }

    public static IServiceCollection AddRocksDbBackend(this IServiceCollection services)
    {
        services.AddSingleton<IStoreBackend, Backend>();
        return services;
    }
}
