using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using FlatSharp;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

[MemoryDiagnoser]
public class ColumnBenchmarks
{
    private readonly IReadable _unpacked;
    private readonly IReadable _packed;
    private readonly IReadable _onHeap;
    private readonly ulong[] _dest;

    public ColumnBenchmarks()
    {
        var unpacked = Appendable.Create(1024);
        _unpacked = unpacked;
        for (ulong i = 0; i < 1024; i++)
        {
            unpacked.Append(i);
        }

        var packed = (ULongPackedColumn)((IUnpacked)_unpacked).Pack();
        _packed = packed;

        var writer = new PooledMemoryBufferWriter();
        ULongPackedColumn.Serializer.Write(writer, packed);

        _onHeap = ULongPackedColumn.Serializer.Parse(writer.WrittenMemory);

        _dest = new ulong[1024];

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
    public ulong UnpackedCopy()
    {
        _unpacked.CopyTo(0, _dest.AsSpan());
        return _dest[^1];
    }

    [Benchmark]
    public ulong Packed()
    {
        var sum = 0UL;
        for (var i = 0; i < _packed.Length; i++)
        {
            sum += _packed[i];
        }

        return sum;
    }

    [Benchmark]
    public ulong PackedCopy()
    {
        _packed.CopyTo(0, _dest.AsSpan());
        return _dest[^1];
    }

    [Benchmark]
    public ulong OnHeap()
    {
        var sum = 0UL;
        for (var i = 0; i < _onHeap.Length; i++)
        {
            sum += _onHeap[i];
        }

        return sum;
    }

    [Benchmark]
    public ulong OnHeapCopy()
    {
        _onHeap.CopyTo(0, _dest.AsSpan());
        return _dest[^1];
    }

}
