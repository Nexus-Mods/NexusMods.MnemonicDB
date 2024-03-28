using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;
using NexusMods.EventSourcing.TestModel.ValueSerializers;
using FileAttributes = NexusMods.EventSourcing.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.EventSourcing.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services)
    {
        services.AddAttributeCollection<FileAttributes>()
            .AddAttributeCollection<ArchiveFileAttributes>()
            .AddAttributeCollection<LoadoutAttributes>()
            .AddAttributeCollection<ModAttributes>();

        services.AddValueSerializer<RelativePathSerializer>()
            .AddValueSerializer<SizeSerializer>()
            .AddValueSerializer<HashSerializer>()
            .AddValueSerializer<UriSerializer>();

        return services;
    }
}
