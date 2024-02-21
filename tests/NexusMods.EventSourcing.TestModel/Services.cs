using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model.Attributes;
namespace NexusMods.EventSourcing.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services) =>
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
}
