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
public class EntityContextBenchmarks : ABenchmark
{
    private EntityId<Loadout>[] _ids = Array.Empty<EntityId<Loadout>>();
    private EntityContext _context = null!;

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
        _context = new EntityContext(EventStore);

        _ids = new EntityId<Loadout>[EntityCount];

        // Pre-create a list of index updaters, then reuse them for each event.
        var indexUpdaters = new (IIndexableAttribute, IAccumulator)[]
        {
            (IEntity.EntityIdAttribute, EntityIdDefinitionAccumulator.From(LoadoutRegistry.SingletonId.Value)),
            // We'll swap this value out each time we update the entity
            (IEntity.EntityIdAttribute, EntityIdDefinitionAccumulator.From(LoadoutRegistry.SingletonId.Value)),
        };

        for (var e = 0; e < EntityCount; e++)
        {
            var evt = new CreateLoadout(EntityId<Loadout>.NewId(), $"Loadout {e}");
            ((EntityIdDefinitionAccumulator)indexUpdaters[1].Item2).Id = evt.Id.Value;
            EventStore.Add(evt, indexUpdaters);
            _ids[e] = evt.Id;
        }


        for (var ev = 0; ev < EventCount; ev++)
        {
            for (var e = 0; e < EntityCount; e++)
            {
                ((EntityIdDefinitionAccumulator)indexUpdaters[1].Item2).Id = _ids[e].Value;
                EventStore.Add(new RenameLoadout(_ids[e], $"Loadout {e} {ev}"), indexUpdaters);
            }
        }
    }

    [IterationSetup]
    public void Cleanup()
    {
        _context.EmptyCaches();
    }

    [Benchmark]
    public void LoadAllEntities()
    {
        var total = 0;
        var registry = _context.Get<LoadoutRegistry>();
        foreach (var loadout in registry.Loadouts)
        {
            total += loadout.Name.Length;
        }
    }

}
