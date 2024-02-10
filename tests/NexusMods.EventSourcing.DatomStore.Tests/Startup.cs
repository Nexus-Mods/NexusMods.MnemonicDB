using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.TestModel;

namespace NexusMods.EventSourcing.DatomStore.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel();
    }
}
