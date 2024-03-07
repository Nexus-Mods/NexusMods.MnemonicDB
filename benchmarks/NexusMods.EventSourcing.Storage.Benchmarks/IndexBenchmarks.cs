using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Tests;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

public class IndexBenchmarks : AStorageBenchmark
{
    private List<AppendableNode> _nodes = null!;
    private IDatomComparator _sorter = null!;
    private IDataNode _preBuilt = null!;
    private Datom _midPoint;

    //[Params(2, 128, 1024)]
    [Params(1024)]
    public ulong Count { get; set; }

    //[Params(2, 128, 1024)]
    [Params(1024)]
    public ulong TxCount { get; set; }

    [Params(SortOrders.EATV, SortOrders.AVTE, SortOrders.AETV)]
    public SortOrders SortOrder { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _nodes = new List<AppendableNode>();

        for (ulong node = 0; node < TxCount; node++)
        {
            _nodes.Add(new AppendableNode());
        }

        var emitters = new Action<EntityId, TxId, ulong>[]
        {
            (e, tx, v) => _registry.Append<TestAttributes.FileHash, ulong>(_nodes[(int)tx.Value], e, tx, DatomFlags.Added, v),
            (e, tx, v) => _registry.Append<TestAttributes.FileName, string>(_nodes[(int)tx.Value], e, tx, DatomFlags.Added, "file " + v),
        };

        for (ulong e = 0; e < Count; e++)
        {
            for (var a = 0; a < 2; a ++)
            {
                for (ulong tx = 0; tx < TxCount; tx++)
                {
                    emitters[a](EntityId.From(e), TxId.From(tx), tx);
                }
            }
        }

        _sorter = _registry.CreateComparator(SortOrder);

        foreach (var node in _nodes)
        {
            node.Sort(_sorter);
        }

        _preBuilt = IndexAll().Flush(NodeStore);
        _midPoint = _preBuilt[(int)((float)_preBuilt.Length / 1.75)];
    }

    //[Benchmark]
    public AppendableIndexNode IndexAll()
    {
        var index = new AppendableIndexNode(_sorter);
        foreach (var node in _nodes)
        {
            index = index.Ingest(node);
        }

        return index;
    }

    [Benchmark]
    public int BinarySearch()
    {
        return _preBuilt.Find(0, _preBuilt.Length, _midPoint, SortOrder, _registry);
    }
}
