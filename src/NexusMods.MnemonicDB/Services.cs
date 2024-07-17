using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Extension methods for adding attributes and other types to the service collection.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Registers the event sourcing services with the service collection.
    /// </summary>
    public static IServiceCollection AddMnemonicDB(this IServiceCollection services)
    {
        services.AddSingleton<IConnection, Connection>();
        services.AddMnemonicDBStorage();

        return services;
    }
    
    /// <summary>
    /// Adds the MnemonicDB storage services to the service collection.
    /// </summary>
    public static IServiceCollection AddMnemonicDBStorage(this IServiceCollection services)
    {
        services.AddAttributeDefinitionModel()
                .AddTransactionModel()
                .AddSingleton<AttributeRegistry>()
                .AddSingleton<IDatomStore, DatomStore>()
                .AddSingleton<DatomStore>(s => (DatomStore)s.GetRequiredService<IDatomStore>());
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
