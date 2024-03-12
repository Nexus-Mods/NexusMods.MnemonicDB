using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using FlatSharp;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

[MemoryDiagnoser]
public class ColumnBenchmarks
{
    private readonly IReadable<ulong> _unpacked;
    private readonly IReadable<ulong> _packed;
    private readonly IReadable<ulong> _onHeap;

    public ColumnBenchmarks()
    {
        var unpacked = Appendable<ulong>.Create(1024);
        _unpacked = unpacked;
        for (ulong i = 0; i < 1024; i++)
        {
            unpacked.Append(i);
        }

        var packed = (ULongPackedColumn)((IUnpacked<ulong>)_unpacked).Pack();
        _packed = packed;

        var writer = new PooledMemoryBufferWriter();
        ULongPackedColumn.Serializer.Write(writer, packed);

        _onHeap = ULongPackedColumn.Serializer.Parse(writer.WrittenMemory);

    }

    [Benchmark]
    public ulong Unpacked()
    {
        var sum = 0UL;
        for (var i = 0; i < _unpacked.Length; i++)
        {
            sum += _unpacked[i];
        }

        return sum;
    }

    [Benchmark]
    public ulong OnHeapPacked()
    {
        var sum = 0UL;
        for (var i = 0; i < _packed.Length; i++)
        {
            sum += _packed[i];
        }

        return sum;
    }

    [Benchmark]
    public ulong OffHeapPacked()
    {
        var sum = 0UL;
        for (var i = 0; i < _onHeap.Length; i++)
        {
            sum += _onHeap[i];
        }

        return sum;
    }

}
