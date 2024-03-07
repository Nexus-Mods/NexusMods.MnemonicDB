using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Serializers;
using NexusMods.EventSourcing.Storage.Sorters;
using NexusMods.EventSourcing.Storage.Tests;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

[MemoryDiagnoser]
public class AppendableNodeBenchmarks
{
    private readonly IServiceProvider _services;
    private readonly AttributeRegistry _registry;
    private AppendableNode _node = null!;

    public AppendableNodeBenchmarks()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(s =>
            {
                s.AddEventSourcingStorage()
                    .AddSingleton<AttributeRegistry>()
                    .AddSingleton<IKvStore, InMemoryKvStore>()
                    .AddAttribute<TestAttributes.FileHash>()
                    .AddAttribute<TestAttributes.FileName>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();
        _services = host.Services;

        _registry = _services.GetRequiredService<AttributeRegistry>();

        _registry.Populate(new[]
        {
            new DbAttribute(Symbol.Intern<TestAttributes.FileHash>(), AttributeId.From(10), Symbol.Intern<UInt64Serializer>()),
            new DbAttribute(Symbol.Intern<TestAttributes.FileName>(), AttributeId.From(11), Symbol.Intern<StringSerializer>())
        });

    }

    [IterationSetup]
    public void IterationSetup()
    {
        _node = new AppendableNode();

        for (ulong e = 0; e < EntityCount; e++)
        {
            for (var a = 0; a < 2; a++)
            {
                for (ulong v = 0; v < 3; v++)
                {
                    if (a == 0)
                        _registry.Append<TestAttributes.FileHash, ulong>(_node, EntityId.From(e), TxId.From(v), DatomFlags.Added, v);
                    else
                        _registry.Append<TestAttributes.FileName, string>(_node, EntityId.From(e), TxId.From(v), DatomFlags.Added, "file " + v);
                }
            }
        }
    }


    [Params(1, 128, 1024)]
    public ulong EntityCount { get; set; }

    [Benchmark]
    public void SortNode()
    {
        var comparator = new EATV(_registry);
        _node.Sort(comparator);

    }
}
