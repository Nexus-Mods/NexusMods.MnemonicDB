using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services)
    {
        services.AddAttributeCollection(typeof(File))
            .AddAttributeCollection(typeof(ArchiveFile))
            .AddAttributeCollection(typeof(Loadout))
            .AddAttributeCollection(typeof(Mod))
            .AddAttributeCollection(typeof(Collection));

        return services;
    }
}
