using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.EventSourcing.Storage.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventSourcingStorage()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddAttribute<ModAttributes.Name>();
    }
}
