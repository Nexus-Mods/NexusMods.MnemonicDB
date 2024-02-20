using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.DatomStore;

namespace NexusMods.EventSourcing.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddDatomStore();
    }
}
