using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using Xunit.DependencyInjection.Logging;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddAttributeCollection(typeof(BuiltInAttributes))
            .AddSingleton<AttributeRegistry>()
            .AddSingleton<Backend>()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddAttributeCollection(typeof(File))
            .AddAttributeCollection(typeof(Mod))
            .AddAttributeCollection(typeof(Collection))
            .AddAttributeCollection(typeof(Loadout))
            .AddAttributeCollection(typeof(Blobs));
    }
}

