using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB.Storage;

public static class Services
{
    public static IServiceCollection AddMnemonicDBStorage(this IServiceCollection services)
    {
        services.AddAttributeCollection(typeof(BuiltInAttributes));
        services.AddSingleton<AttributeRegistry>();
        services.AddSingleton<IDatomStore, DatomStore>();
        services.AddSingleton<DatomStore>(s => (DatomStore)s.GetRequiredService<IDatomStore>());
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
