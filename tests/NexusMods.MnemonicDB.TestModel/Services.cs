using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services)
    {
        services.AddModel<IFile>()
            .AddModel<IArchiveFile>()
            .AddModel<ILoadout>()
            .AddModel<IMod>()
            .AddModel<ICollection>();

        return services;
    }
}
