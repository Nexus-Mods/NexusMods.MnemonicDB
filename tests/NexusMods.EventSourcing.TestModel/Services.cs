using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model.Attributes;
using NexusMods.EventSourcing.TestModel.ValueSerializers;
using FileAttributes = NexusMods.EventSourcing.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.EventSourcing.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services)
    {
        services.AddAttribute<ModFileAttributes.Path>()
            .AddAttribute<ModFileAttributes.Hash>()
            .AddAttribute<ModFileAttributes.Index>()
            .AddAttribute<ArchiveFileAttributes.Path>()
            .AddAttribute<ArchiveFileAttributes.ArchiveHash>()
            .AddAttribute<LoadoutAttributes.Name>()
            .AddAttribute<LoadoutAttributes.UpdatedTx>()
            .AddAttribute<ModAttributes.Name>()
            .AddAttribute<ModAttributes.UpdatedTx>()
            .AddAttribute<ModAttributes.LoadoutId>();

        services.AddAttributeCollection<FileAttributes>()
            .AddAttributeCollection<ComplexModel.Attributes.LoadoutAttributes>()
            .AddAttributeCollection<ComplexModel.Attributes.ModAttributes>();

        services.AddValueSerializer<RelativePathSerializer>()
            .AddValueSerializer<SizeSerializer>()
            .AddValueSerializer<HashSerializer>()
            .AddValueSerializer<UriSerializer>();

        return services;
    }
}
