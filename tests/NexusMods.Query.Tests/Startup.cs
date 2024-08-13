using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.InMemoryBackend;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Query.Tests.Models;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Query.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddMnemonicDB()
            .AddLogging(l => l.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddDatomStoreSettings(new DatomStoreSettings()
            {
                Path = default,
            })
            .AddSolarSystemModel()
            .AddSingleton<IStoreBackend, Backend>();

    }
}
