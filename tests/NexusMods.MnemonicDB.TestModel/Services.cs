using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.MnemonicDB.TestModel.ValueSerializers;
using FileAttributes = NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MnemonicDB.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services)
    {
        services.AddAttributeCollection<FileAttributes>()
            .AddAttributeCollection<ArchiveFileAttributes>()
            .AddAttributeCollection<LoadoutAttributes>()
            .AddAttributeCollection<ModAttributes>()
            .AddAttributeCollection<CollectionAttributes>();

        services.AddValueSerializer<RelativePathSerializer>()
            .AddValueSerializer<SizeSerializer>()
            .AddValueSerializer<HashSerializer>()
            .AddValueSerializer<UriSerializer>();

        return services;
    }
}
