using Microsoft.Extensions.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.MnemonicDB.Queryable.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(l => l.AddXunitOutput());
    }
}

