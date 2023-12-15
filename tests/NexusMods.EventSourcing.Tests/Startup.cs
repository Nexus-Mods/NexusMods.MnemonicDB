using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.Tests.Contexts;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.EventSourcing.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleton<IEventStore, InMemoryEventStore<EventSerializer>>()
            .AddEvents()
            .AddEventSourcing()

            .AddLogging(builder => builder.AddXunitOutput());
    }
}
