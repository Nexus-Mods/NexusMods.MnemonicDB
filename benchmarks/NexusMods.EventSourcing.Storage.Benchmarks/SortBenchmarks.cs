using System;
using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Tests;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

[MemoryDiagnoser]
public class SortBenchmarks : AStorageBenchmark
{
    private AppendableNode _node = null!;
    private IDatomComparator _sorter = null!;

    [Params(10, 1024, 1024 * 8)]
    public ulong Count { get; set; }

    [Params(SortOrders.EATV, SortOrders.AVTE, SortOrders.AETV)]
    public SortOrders SortOrder { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var node = new AppendableNode();

        var emitters = new Action<EntityId, TxId, ulong>[]
        {
            (e, tx, v) => _registry.Append<TestAttributes.FileHash, ulong>(node, e, tx, DatomFlags.Added, v),
            (e, tx, v) => _registry.Append<TestAttributes.FileName, string>(node, e, tx, DatomFlags.Added, "file " + v),
        };

        for (ulong e = 0; e < Count; e++)
        {
            for (var a = 0; a < 2; a ++)
            {
                for (ulong tx = 0; tx < 3; tx++)
                {
                    for (ulong v = 0; v < 3; v++)
                    {
                        emitters[a](EntityId.From(e), TxId.From(tx), v);
                    }
                }
            }
        }

        _node = node;

    }


    [Benchmark]
    public AppendableNode Sort()
    {
        _sorter = _registry.CreateComparator(SortOrder);
        _node.Sort(_sorter);
        return _node;
    }
}
