using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.TestModel;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.MnemonicDB.QueryableParser.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug));
    }
}
