using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Tests;

public static class Startup
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        return services
            .AddAttributeDefinitionModel()
            .AddAdapters()
            .AddTransactionModel()
            .AddTestModel()
            .AddSingleton<TemporaryFileManager>()
            .AddFileSystem()
            .AddLogging(builder => builder.AddConsole());
    }
}
