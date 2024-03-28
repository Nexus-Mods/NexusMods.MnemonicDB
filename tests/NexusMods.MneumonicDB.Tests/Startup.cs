using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MneumonicDB.Storage;
using NexusMods.MneumonicDB.TestModel;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.MneumonicDB.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddMneumonicDBStorage();
    }
}
