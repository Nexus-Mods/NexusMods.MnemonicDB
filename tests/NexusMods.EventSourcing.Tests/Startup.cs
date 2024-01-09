using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.EventSourcing.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleton<BinaryEventSerializer>()
            .AddSingleton<IEventStore, InMemoryEventStore<EventSerializer>>()
            .AddEvents()
            .AddEvent<SimpleTestEvent>()
            .AddEventSourcing()

            .AddLogging(builder => builder.AddXunitOutput());
    }
}
