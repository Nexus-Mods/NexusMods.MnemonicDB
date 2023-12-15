using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.FasterKV;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Benchmarks;

public class EntityContextBenchmarks
{
    private readonly IServiceProvider _services;
    private IEventStore _eventStore = null!;
    private EntityId<Loadout>[] _ids = Array.Empty<EntityId<Loadout>>();
    private EntityContext _context = null!;

    [Params(typeof(InMemoryEventStore<EventSerializer>),
        typeof(FasterKVEventStore<EventSerializer>))]
    public Type EventStoreType { get; set; } = typeof(InMemoryEventStore<EventSerializer>);

    [Params(100, 1000)]
    public int EventCount { get; set; }

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    public EntityContextBenchmarks()
    {
        var host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddEventSourcing()
                    .AddEvents();
            })
            .Build();

        _services = host.Services;
    }

    [GlobalSetup]
    public void Setup()
    {
        if (EventStoreType == typeof(InMemoryEventStore<EventSerializer>))
        {
            _eventStore = new InMemoryEventStore<EventSerializer>(_services.GetRequiredService<EventSerializer>());
        }
        else if (EventStoreType == typeof(FasterKVEventStore<EventSerializer>))
        {
            _eventStore = new FasterKVEventStore<EventSerializer>(_services.GetRequiredService<EventSerializer>(),
                new Settings
            {
                StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("FasterKV.EventStore" + Guid.NewGuid())
            });
        }
        else
        {
            throw new NotSupportedException($"EventStoreType '{EventStoreType}' is not supported.");
        }

        _context = new EntityContext(_eventStore);

        _ids = new EntityId<Loadout>[EntityCount];
        for (var e = 0; e < EntityCount; e++)
        {
            var evt = new CreateLoadout(EntityId<Loadout>.NewId(), $"Loadout {e}");
            _eventStore.Add(evt);
            _ids[e] = evt.Id;
        }


        for (var ev = 0; ev < EventCount; ev++)
        {
            for (var e = 0; e < EntityCount; e++)
            {
                _eventStore.Add(new RenameLoadout(_ids[e], $"Loadout {e} {ev}"));
            }
        }
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
