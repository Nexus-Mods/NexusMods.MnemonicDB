using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks;

[MemoryDiagnoser]
public class SerializationBenchmarks : ABenchmark
{
    private readonly IEvent[] _events;
    private BinaryEventSerializer _serializer = null!;
    private byte[][] _serializedEvents = null!;

    public SerializationBenchmarks()
    {
        _events =
        [
            new CreateLoadout(EntityId<Loadout>.NewId(), "Test"),
            new RenameLoadout(EntityId<Loadout>.NewId(), "Test"),
            new AddMod("New Mod", true, EntityId<Mod>.NewId(), new EntityId<Loadout>()),
            new AddCollection(EntityId<Collection>.NewId(), "NewCollection", EntityId<Loadout>.NewId(),
                [EntityId<Mod>.NewId()]),
            new DeleteMod(EntityId<Mod>.NewId(), EntityId<Loadout>.NewId()),
            new RenameLoadout(EntityId<Loadout>.NewId(), "Test"),
            new SwapModEnabled(EntityId<Mod>.NewId(), true)
        ];
    }

    [GlobalSetup]
    public void Setup()
    {
        _serializer = Services.GetRequiredService<BinaryEventSerializer>();

        _serializedEvents = _events.Select(evnt => _serializer.Serialize(evnt).ToArray()).ToArray();
    }

    [Benchmark]
    public int Serialize()
    {
        var size = 0;
        for (var i = 0; i < _events.Length; i++)
        {
            var evnt = _events[i];
            size += _serializer.Serialize(evnt).Length;
        }

        return size;
    }

    [Benchmark]
    public int Deserialize()
    {
        var size = 0;
        for (var i = 0; i < _serializedEvents.Length; i++)
        {
            var evnt = _serializedEvents[i];
            _serializer.Deserialize(evnt);
            size += 1;
        }
        return size;
    }

}
