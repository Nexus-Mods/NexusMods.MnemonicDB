using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks;

[MemoryDiagnoser]
public class EventStoreBenchmarks : ABenchmark
{
    private readonly EntityId<Loadout>[] _ids;
    private readonly RenameLoadout[] _events;

    public EventStoreBenchmarks()
    {
        _ids = Enumerable.Range(0, 100)
            .Select(_ => EntityId<Loadout>.NewId())
            .ToArray();

        _events = _ids
            .Select(id => new RenameLoadout(id, "Loadout"))
            .ToArray();

    }


    [Params(typeof(InMemoryEventStore<BinaryEventSerializer>),
        //typeof(FasterKVEventStore<BinaryEventSerializer>),
        typeof(RocksDBEventStore<BinaryEventSerializer>))]
    public Type EventStoreType { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        MakeStore(EventStoreType);

        foreach (var evEvent in _events)
        {
            EventStore.Add(evEvent);
        }
    }

    [Benchmark]
    public void AddEvent()
    {
        var rndEvent = _events[Random.Shared.Next(0, _events.Length)];
        EventStore.Add(rndEvent);
    }


    [Benchmark]
    public void ReadEvents()
    {
        var ingester = new EventCounter();
        EventStore.EventsForEntity(_ids[Random.Shared.Next(0, _ids.Length)].Value, ingester);
    }

    private struct EventCounter : IEventIngester
    {
        public int Count { get; private set; }

        public bool Ingest(TransactionId id, IEvent @event)
        {
            Count++;
            return true;
        }
    }


}
