using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.MnemonicDB.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddSingleton<TemporaryFileManager>()
            .AddFileSystem()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddMnemonicDBStorage();
    }
}
