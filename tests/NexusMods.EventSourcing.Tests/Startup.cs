using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.TestModel;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.EventSourcing.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddEventSourcingStorage();
    }
}
