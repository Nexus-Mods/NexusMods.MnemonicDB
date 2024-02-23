using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.TestModel;

namespace NexusMods.EventSourcing.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddEventSourcingStorage();
    }
}
