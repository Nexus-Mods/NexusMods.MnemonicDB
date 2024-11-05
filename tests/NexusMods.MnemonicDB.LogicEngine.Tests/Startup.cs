using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.TestModel;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.MnemonicDB.LogicEngine.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMnemonicDBStorage()
            .AddTestModel()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug));
    }
}
