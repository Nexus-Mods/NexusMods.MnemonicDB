using System;
using BenchmarkDotNet.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class IndexSegmentEBenchmarks
{
    private readonly IndexSegment _index;

    public IndexSegmentEBenchmarks()
    {
        var registry = new AttributeCache(); 
        using var builder = new IndexSegmentBuilder(registry);

        for (var e = 1; e < 100; e++)
        {
            for (var a = 0; a < 20; a++)
            {
                builder.Add(new Datom(new KeyPrefix(EntityId.From((ulong)e), AttributeId.From((ushort)a), TxId.From((ulong)(e + a)), false,
                    ValueTag.Null), ReadOnlyMemory<byte>.Empty));
            }
        }

        _index = builder.Build();
    }

    [Params(0, 1, 2, 3, 10, 71, 99)] public int ToFind { get; set; }


    [Benchmark]
    public int FindLinear()
    {
        var find = EntityId.From((ulong)ToFind);
        for (var i = 0; i < _index.Count; i++)
        {
            var datom = _index[i];
            if (datom.E == find)
                return i;
        }
        return -1;
    }

    [Benchmark]
    public int FindBinarySearch()
    {
        var find = EntityId.From((ulong)ToFind);

        var left = 0;
        var right = _index.Count - 1;
        var result = -1;
        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var datom = _index[mid];
            var comparison = datom.E.CompareTo(find);
            if (comparison == 0)
            {
                result = mid; // Don't return, but continue searching to the left
                right = mid - 1;
            }
            else if (comparison < 0)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        return result; // Return the first occurrence found, or -1 if not found
    }
}
