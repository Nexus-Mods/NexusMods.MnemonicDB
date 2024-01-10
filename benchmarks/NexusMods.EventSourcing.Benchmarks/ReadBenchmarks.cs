using System;
using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks;

[MemoryDiagnoser]
public class ReadBenchmarks : ABenchmark
{
    private EntityId<Loadout>[] _ids = Array.Empty<EntityId<Loadout>>();

    [Params(typeof(InMemoryEventStore<BinaryEventSerializer>),
        //typeof(FasterKVEventStore<BinaryEventSerializer>),
        typeof(RocksDBEventStore<BinaryEventSerializer>))]
    public Type EventStoreType { get; set; } = typeof(InMemoryEventStore<BinaryEventSerializer>);

    [Params(100, 1000)]
    public int EventCount { get; set; }

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        MakeStore(EventStoreType);

        _ids = new EntityId<Loadout>[EntityCount];
        for (var e = 0; e < EntityCount; e++)
        {
            var evt = new CreateLoadout(EntityId<Loadout>.NewId(), $"Loadout {e}");
            EventStore.Add(evt);
            _ids[e] = evt.Id;
        }


        for (var ev = 0; ev < EventCount; ev++)
        {
            for (var e = 0; e < EntityCount; e++)
            {
                EventStore.Add(new RenameLoadout(_ids[e], $"Loadout {e} {ev}"));
            }
        }
    }

    [Benchmark]
    public void ReadEvents()
    {

        var ingester = new Counter();
        EventStore.EventsForEntity(_ids[_ids.Length/2].Value, ingester);
    }

    private class Counter : IEventIngester
    {
        public int Count { get; private set; }
        public bool Ingest(TransactionId _, IEvent @event)
        {
            Count++;
            return true;
        }
    }
}
