using Microsoft.Extensions.DependencyInjection;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.MneumonicDB.TestModel.ValueSerializers;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MneumonicDB.TestModel;

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
