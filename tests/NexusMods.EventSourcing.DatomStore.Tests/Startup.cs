using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.EventSourcing.DatomStore.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTestModel()
            .AddDatomStore();
    }
}
