using Microsoft.Extensions.DependencyInjection;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Storage.Abstractions;
using NexusMods.MneumonicDB.Storage.RocksDbBackend;
using NexusMods.MneumonicDB.Storage.Serializers;

namespace NexusMods.MneumonicDB.Storage;

public static class Services
{
    public static IServiceCollection AddMneumonicDBStorage(this IServiceCollection services)
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
