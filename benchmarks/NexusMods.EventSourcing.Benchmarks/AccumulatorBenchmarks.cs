using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;
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

    public AccumulatorBenchmarks()
    {
        MakeStore(typeof(InMemoryEventStore<EventSerializer>));

        _ctx = new EntityContext(EventStore);
        _ctx.Add(new CreateLoadout(EntityId<Loadout>.NewId(), "Test"));
        _ctx.Add(new CreateLoadout(EntityId<Loadout>.NewId(), "Test2"));

        _registry = _ctx.Get<LoadoutRegistry>();
        if (_registry.Loadouts.Count != 2)
            throw new Exception("Bad state");

        _loadouts = _registry.Loadouts.ToArray();

    }

    [Benchmark]
    public string GetMultiAttributeItems()
    {
        return _loadouts[0].Name;
    }
}
