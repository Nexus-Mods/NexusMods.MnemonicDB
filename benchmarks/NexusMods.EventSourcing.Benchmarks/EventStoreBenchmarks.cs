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
    private readonly (IIndexableAttribute, IAccumulator)[] _indexUpdaters;

    public EventStoreBenchmarks()
    {
        _ids = Enumerable.Range(0, 100)
            .Select(_ => EntityId<Loadout>.NewId())
            .ToArray();

        _events = _ids
            .Select(id => new RenameLoadout(id, "Loadout"))
            .ToArray();

        // Pre-create a list of index updaters, then reuse them for each event.
        _indexUpdaters =
        [
            (IEntity.EntityIdAttribute, EntityIdDefinitionAccumulator.From(LoadoutRegistry.SingletonId.Id)),
            // We'll swap this value out each time we update the entity
            (IEntity.EntityIdAttribute, EntityIdDefinitionAccumulator.From(LoadoutRegistry.SingletonId.Id))
        ];

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
            ((EntityIdDefinitionAccumulator)_indexUpdaters[1].Item2).Id = evEvent.Id.Id;
            EventStore.Add(evEvent, _indexUpdaters);
        }
    }

    [Benchmark]
    public void AddEvent()
    {
        var rndEvent = _events[Random.Shared.Next(0, _events.Length)];
        _indexUpdaters[1].Item2 = EntityIdDefinitionAccumulator.From(rndEvent.Id.Id);
        EventStore.Add(rndEvent, _indexUpdaters);
    }


    [Benchmark]
    public void ReadEvents()
    {
        var ingester = new EventCounter();
        var idx = Random.Shared.Next(0, _ids.Length);
        EventStore.EventsForIndex(IEntity.EntityIdAttribute, _ids[idx].Id, ingester);
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
