using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model.Attributes;


public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services) =>
        services.AddAttribute<ModFileAttributes.Path>()
                .AddAttribute<ModFileAttributes.Hash>()
                .AddAttribute<ModFileAttributes.Index>()

            ;
        /*
            .AddAttribute<Model.Attributes.ArchiveFile.Path>()
            .AddAttribute<Model.Attributes.ArchiveFile.Index>();*/
    //.AddReadModel<File>()
    //.AddReadModel<ArchiveFile>();
}
