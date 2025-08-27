using fff;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.MnemonicDB.TestModel;

public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services) =>
        services
            .AddFileModel()
            .AddArchiveFileModel()
            .AddModModel()
            .AddLoadoutModel()
            .AddCollectionModel()
            .AddParentAModel()
            .AddParentBModel()
            .AddChildModel()
            .AddExampleQueriesSql();
}
