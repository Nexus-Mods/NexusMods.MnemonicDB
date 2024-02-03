using Microsoft.Extensions.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.EventSourcing.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddEventSourcing()

            .AddLogging(builder => builder.AddXunitOutput());
    }
}
