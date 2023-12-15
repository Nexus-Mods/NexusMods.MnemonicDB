using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.FasterKV;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.TestModel;
using NexusMods.Paths;
using Settings = NexusMods.EventSourcing.FasterKV.Settings;

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
        else if (type == typeof(FasterKVEventStore<EventSerializer>))
        {
            eventStore = new FasterKVEventStore<EventSerializer>(serializer,
                new Settings
                {
                    StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("FasterKV.EventStore" + Guid.NewGuid())
                });
        }
        else if (type == typeof(RocksDBEventStore<EventSerializer>))
        {
            eventStore = new RocksDBEventStore<EventSerializer>(serializer,
                new RocksDB.Settings
                {
                    StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("FasterKV.EventStore" + Guid.NewGuid())
                });
        }
        else
        {
            throw new NotSupportedException($"EventStoreType '{type}' is not supported.");
        }

        EventStore = eventStore;
    }

}
