using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.InMemoryBackend;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.Query.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddMnemonicDB()
            .AddDatomStoreSettings(new DatomStoreSettings()
            {
                Path = default,
            })
            .AddSingleton<IStoreBackend, Backend>();

    }
}
