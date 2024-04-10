using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel.ValueSerializers;

namespace NexusMods.MnemonicDB.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services)
    {
        services.AddAttributeCollection<File>()
            .AddAttributeCollection<ArchiveFile>()
            .AddAttributeCollection<Loadout>()
            .AddAttributeCollection<Mod>()
            .AddAttributeCollection<Collection>();

        services.AddValueSerializer<RelativePathSerializer>()
            .AddValueSerializer<SizeSerializer>()
            .AddValueSerializer<HashSerializer>()
            .AddValueSerializer<UriSerializer>();

        return services;
    }
}
