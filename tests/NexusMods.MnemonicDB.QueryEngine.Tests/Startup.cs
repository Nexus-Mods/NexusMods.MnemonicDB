using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.LargeTestModel;
using NexusMods.MnemonicDB.LargeTestModel.Models;
using NexusMods.Paths;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.MnemonicDB.QueryEngine.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var storePath = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("model").Combine(Guid.NewGuid().ToString());
        storePath.CreateDirectory();
        
        services.AddLargeTestModel()
            .AddMnemonicDB()
            .AddDatomStoreSettings(new DatomStoreSettings()
            {
                Path = storePath
            })
            .AddRocksDbBackend()
            .AddFileSystem()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug));

    }
}

