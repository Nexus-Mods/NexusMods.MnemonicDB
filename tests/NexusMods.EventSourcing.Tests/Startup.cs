using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.TestModel;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.EventSourcing.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {

        container
            .AddEventSourcing()
            .AddTestModel()
            .AddLogging(builder => builder.AddXunitOutput());
    }
}
