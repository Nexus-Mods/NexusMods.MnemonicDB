using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
using NexusMods.MnemonicDB.TestModel;
using Transaction = NexusMods.MnemonicDB.Abstractions.BuiltInEntities.Transaction;

namespace NexusMods.MnemonicDB.Storage.Tests;

public static class Startup
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services) =>
        services
            .AddAttributeCollection(typeof(AttributeDefinition))
            .AddAttributeCollection(typeof(Transaction))
            .AddTestModel()
            .AddSingleton<IAttribute>(Blobs.InKeyBlob)
            .AddSingleton<IAttribute>(Blobs.InValueBlob)
            .AddSingleton<Backend>()
            .AddLogging(builder => builder.AddConsole());
}

