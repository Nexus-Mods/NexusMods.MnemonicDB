using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Tests;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

public class IndexBenchmarks : AStorageBenchmark
{
    private List<AppendableNode> _chunks = null!;
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
        _chunks = new List<AppendableNode>();

        for (ulong chunk = 0; chunk < TxCount; chunk++)
        {
            _chunks.Add(new AppendableNode());
        }

        var emitters = new Action<EntityId, TxId, ulong>[]
        {
            (e, tx, v) => _registry.Append<TestAttributes.FileHash, ulong>(_chunks[(int)tx.Value], e, tx, DatomFlags.Added, v),
            (e, tx, v) => _registry.Append<TestAttributes.FileName, string>(_chunks[(int)tx.Value], e, tx, DatomFlags.Added, "file " + v),
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

        foreach (var chunk in _chunks)
        {
            chunk.Sort(_sorter);
        }

        _preBuilt = IndexAll().Flush(NodeStore);
        _midPoint = _preBuilt[(int)((float)_preBuilt.Length / 1.75)];
    }

    //[Benchmark]
    public AppendableIndexNode IndexAll()
    {
        var index = new AppendableIndexNode(_sorter);
        foreach (var chunk in _chunks)
        {
            index = index.Ingest(chunk);
        }

        return index;
    }

    [Benchmark]
    public int BinarySearch()
    {
        return _preBuilt.Find(0, _preBuilt.Length, _midPoint, SortOrder, _registry);
    }
}
