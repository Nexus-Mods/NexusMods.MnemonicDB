using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks;

[MemoryDiagnoser]
public class AccumulatorBenchmarks : ABenchmark
{
    private readonly EntityContext _ctx;
    private readonly LoadoutRegistry _registry;
    private readonly Loadout[] _loadouts;
    private readonly int _numLoadouts;

    public AccumulatorBenchmarks()
    {
        MakeStore(typeof(InMemoryEventStore<BinaryEventSerializer>));

        _ctx = new EntityContext(EventStore);
        _ctx.Add(new CreateLoadout(EntityId<Loadout>.NewId(), "Test"));
        _ctx.Add(new CreateLoadout(EntityId<Loadout>.NewId(), "Test2"));

        _registry = _ctx.Get<LoadoutRegistry>();
        if (_registry.Loadouts.Count != 2)
            throw new Exception("Bad state");

        _loadouts = _registry.Loadouts.ToArray();
        _numLoadouts = _loadouts.Length;

    }

    [Benchmark]
    public int GetMultiAttributeItems()
    {
        var size = 0;
        for (var j = 0; j < 10_000_000; j++)
        {
            size += _loadouts[0].Name.Length;
        }
        return size;
    }
}
