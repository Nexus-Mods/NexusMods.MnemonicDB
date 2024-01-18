using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks;

[MemoryDiagnoser]
public class WriteBenchmarks : ABenchmark
{
    private readonly IEvent[] _events;

    [Params(typeof(InMemoryEventStore<BinaryEventSerializer>),
        //typeof(FasterKVEventStore<BinaryEventSerializer>),
        typeof(RocksDBEventStore<BinaryEventSerializer>))]
    public Type EventStoreType { get; set; } = typeof(InMemoryEventStore<BinaryEventSerializer>);

    private EntityContext _context = null!;

    [Params(100, 1000, 10000)]
    public int EventCount { get; set; } = 100;

    public WriteBenchmarks()
    {
        _events =
        [
            new CreateLoadout(EntityId<Loadout>.NewId(), "Loadout 1"),
            new SwapModEnabled(EntityId<Mod>.NewId(), true),
            new DeleteMod(EntityId<Mod>.NewId(), EntityId<Loadout>.NewId())
        ];


    }

    [IterationSetup]
    public void Setup()
    {
        MakeStore(EventStoreType);
        _context = new EntityContext(EventStore);
    }

    [Benchmark]
    public void WriteEvents()
    {
        for (var i = 0; i < EventCount; i++)
        {
            var evnt = _events[i % _events.Length];
            _context.Add(evnt);
        }
    }
}
