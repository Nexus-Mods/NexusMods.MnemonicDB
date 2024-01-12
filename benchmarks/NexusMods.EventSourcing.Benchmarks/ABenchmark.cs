using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;
using NexusMods.Paths;

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
        var serializer = Services.GetRequiredService<BinaryEventSerializer>();
        IEventStore eventStore;

        var registry = Services.GetRequiredService<ISerializationRegistry>();

        if (type == typeof(InMemoryEventStore<BinaryEventSerializer>))
        {
            eventStore = new InMemoryEventStore<BinaryEventSerializer>(serializer, registry);
        }
        else if (type == typeof(RocksDBEventStore<BinaryEventSerializer>))
        {
            eventStore = new RocksDBEventStore<BinaryEventSerializer>(serializer,
                new RocksDB.Settings
                {
                    StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("FasterKV.EventStore" + Guid.NewGuid())
                },
                registry);
        }
        else
        {
            throw new NotSupportedException($"EventStoreType '{type}' is not supported.");
        }

        EventStore = eventStore;
    }

}
