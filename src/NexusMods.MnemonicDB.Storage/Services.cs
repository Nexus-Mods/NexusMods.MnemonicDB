using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Serializers;

namespace NexusMods.MnemonicDB.Storage;

public static class Services
{
    public static IServiceCollection AddMnemonicDBStorage(this IServiceCollection services)
    {
        services.AddValueSerializer<UInt64Serializer>();
        services.AddValueSerializer<StringSerializer>();
        services.AddValueSerializer<SymbolSerializer>();
        services.AddValueSerializer<TxIdSerializer>();
        services.AddValueSerializer<EntityIdSerializer>();
        services.AddAttributeCollection<BuiltInAttributes>();
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
