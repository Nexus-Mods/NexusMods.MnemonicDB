using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB.Storage;

/// <summary>
/// DI services for the MnemonicDB storage.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds the MnemonicDB storage services to the service collection.
    /// </summary>
    public static IServiceCollection AddMnemonicDBStorage(this IServiceCollection services)
    {
        services.AddAttributeCollection(typeof(AttributeDefinition));
        services.AddAttributeCollection(typeof(Transaction));
        services.AddSingleton<AttributeRegistry>();
        services.AddSingleton<IDatomStore, DatomStore>();
        services.AddSingleton<DatomStore>(s => (DatomStore)s.GetRequiredService<IDatomStore>());
        return services;
    }

    /// <summary>
    /// Adds the MnemonicDB storage settings to the service collection.
    /// </summary>
    public static IServiceCollection AddDatomStoreSettings(this IServiceCollection services,
        DatomStoreSettings settings)
    {
        services.AddSingleton(settings);
        return services;
    }

    /// <summary>
    /// Adds the RocksDB backend to the service collection.
    /// </summary>
    public static IServiceCollection AddRocksDbBackend(this IServiceCollection services)
    {
        services.AddSingleton<IStoreBackend, Backend>();
        return services;
    }
}
