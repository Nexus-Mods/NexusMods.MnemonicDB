using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.LargeTestModel.Models;

namespace NexusMods.MnemonicDB.LargeTestModel;

public static class Services
{
    public static IServiceCollection AddLargeTestModel(this IServiceCollection services) =>
        services.AddLargeLoadoutModel()
            .AddGroupModel()
            .AddLoadoutItemModel()
            .AddArchiveModel();

}
