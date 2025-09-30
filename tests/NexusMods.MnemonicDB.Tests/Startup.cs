using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.QueryFunctions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Tests;

public static class Startup
{
    
    
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        return services
            .AddAttributeDefinitionModel()
            .AddTransactionModel()
            .AddTestModel()
            .AddSingleton<TemporaryFileManager>()
            .AddFileSystem()
            .AddAdapters()
            .AddConverters()
            .AddSingleton<IQueryEngine, QueryEngine>()
            .AddSingleton<AScalarFunction, ToStringScalarFn>()
            .AddLogging(builder => builder.AddConsole());
    }
}
