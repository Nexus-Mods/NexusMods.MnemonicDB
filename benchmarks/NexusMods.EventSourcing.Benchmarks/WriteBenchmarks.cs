using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks;

[MemoryDiagnoser]
public class WriteBenchmarks
{
    private readonly IServiceProvider _services;
    private InMemoryEventStore<EventSerializer> _eventStore = null!;
    private readonly IEvent[] _events;

    [Params(typeof(InMemoryEventStore<EventSerializer>))]
    public Type EventStoreType { get; set; } = typeof(InMemoryEventStore<EventSerializer>);

    [Params(100, 1000, 10000)]
    public int EventCount { get; set; } = 100;

    public WriteBenchmarks()
    {
        var host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddEventSourcing()
                    .AddEvents();
            })
            .Build();

        _events = new IEvent[]
        {
            new CreateLoadout(EntityId<Loadout>.NewId(), "Loadout 1"),
            new SwapModEnabled(EntityId<Mod>.NewId(), true),
            new DeleteMod(EntityId<Mod>.NewId(), EntityId<Loadout>.NewId())
        };

        _services = host.Services;
    }

    [IterationSetup]
    public void Setup()
    {
        if (EventStoreType == typeof(InMemoryEventStore<EventSerializer>))
        {
            _eventStore = new InMemoryEventStore<EventSerializer>(_services.GetRequiredService<EventSerializer>());
        }
        else
        {
            throw new NotSupportedException($"EventStoreType '{EventStoreType}' is not supported.");
        }
    }

    [Benchmark]
    public async Task WriteEvents()
    {
        for (var i = 0; i < EventCount; i++)
        {
            var evnt = _events[i % _events.Length];
            await _eventStore.Add(evnt);
        }
    }
}
