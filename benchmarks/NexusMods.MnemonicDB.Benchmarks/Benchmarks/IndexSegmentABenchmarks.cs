using System;
using BenchmarkDotNet.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.Benchmarks.Benchmarks;

public class IndexSegmentABenchmarks
{
    private readonly IndexSegment _index;

    public IndexSegmentABenchmarks()
    {
        var registry = new AttributeRegistry(null!, []);
        using var builder = new IndexSegmentBuilder(registry);

        for (int a = 1; a < 100; a++)
        {
            var prefix = new KeyPrefix(EntityId.From(42), AttributeId.From((ushort)a), TxId.From(42), false,
                ValueTags.Null);
            builder.Add(new Datom(prefix, ReadOnlyMemory<byte>.Empty, registry));
        }

        _index = builder.Build();
    }

    [Params(1, 2, 3, 4, 5, 20, 30, 50, 99)]
    public int ToFind { get; set; }

    [Benchmark]
    public int FindLinear()
    {
        var find = AttributeId.From((ushort)ToFind);
        for (var i = 0; i < _index.Count; i++)
        {
            var datom = _index[i];
            if (datom.A == find)
                return i;
        }
        return -1;
    }


}
