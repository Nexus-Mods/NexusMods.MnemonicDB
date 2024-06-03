using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
using NexusMods.MnemonicDB.TestModel;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddAttributeCollection(typeof(BuiltInAttributes))
            .AddTestModel()
            .AddSingleton<IAttribute>(Blobs.InKeyBlob)
            .AddSingleton<IAttribute>(Blobs.InValueBlob)
            .AddSingleton<AttributeRegistry>()
            .AddSingleton<Backend>()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug));
    }
}

