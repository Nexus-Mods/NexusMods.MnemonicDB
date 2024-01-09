using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel;

namespace NexusMods.EventSourcing.Benchmarks;

public abstract class ABenchmark
{
    protected IServiceProvider Services = null!;

    public ABenchmark()
    {
        var host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddEventSourcing()
                    .AddEvents();
            })
            .Build();

        Services = host.Services;
    }

    protected IEventStore EventStore = null!;

    public void MakeStore(Type type)
    {
        var serializer = Services.GetRequiredService<EventSerializer>();
        IEventStore eventStore;
        if (type == typeof(InMemoryEventStore<EventSerializer>))
        {
            eventStore = new InMemoryEventStore<EventSerializer>(serializer);
        }
        else
        {
            throw new NotSupportedException($"EventStoreType '{type}' is not supported.");
        }

        EventStore = eventStore;
    }

}
