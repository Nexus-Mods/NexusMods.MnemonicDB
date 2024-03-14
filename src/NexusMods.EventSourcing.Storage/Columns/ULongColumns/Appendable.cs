using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Columns.ULongColumns;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// An appendable column of ulong values. This stores values as a auto-expanding array of ulong values.
/// Backed by the shared memory pool
/// </summary>
/// <typeparam name="T"></typeparam>
public class Appendable : IDisposable, IAppendable, IUnpacked
{
    public const int DefaultSize = 16;
    private IMemoryOwner<ulong> _data;
    private int _length;

    private Appendable(IMemoryOwner<ulong> data, int length)
    {
        _data = data;
        _length = length;
    }

    public static Appendable Create(int initialSize = DefaultSize)
    {
        return new Appendable(MemoryPool<ulong>.Shared.Rent(DefaultSize), 0);
    }

    public static Appendable Create(IReadable src, int offset, int length)
    {
        var memory = MemoryPool<ulong>.Shared.Rent(length);
        src.CopyTo(offset, memory.Memory.Span.SliceFast(0, length));
        return new Appendable(memory, length);
    }

    public static Appendable Unpack(IReadable column)
    {
        var node = Create(column.Length);
        column.CopyTo(0, node.GetWritableSpan(column.Length));
        node.SetLength(column.Length);
        return node;
    }

    private Span<ulong> CastedSpan => _data.Memory.Span;


    public void Dispose()
    {
        _data.Dispose();
    }

    public void Append(ulong value)
    {
        Ensure(1);
        _data.Memory.Span[_length] = value;
        _length++;
    }

    private void Ensure(int i)
    {
        if (_length + i <= _data.Memory.Length) return;
        var newData = MemoryPool<ulong>.Shared.Rent(_data.Memory.Length * 2);
        _data.Memory.CopyTo(newData.Memory);
        _data.Dispose();
        _data = newData;

    }

    public void Append(ReadOnlySpan<ulong> values)
    {
        Ensure(values.Length);
        values.CopyTo(CastedSpan.Slice(_length));
        _length += values.Length;
    }

    public void Append(ReadOnlySpan<ulong> values, ReadOnlySpan<ulong> mask)
    {
        Ensure(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            if ((mask[i >> 6] & (1UL << (i & 63))) != 0)
            {
                Append(values[i]);
            }
        }
    }

    public void Append(IEnumerable<ulong> values)
    {
        foreach (var value in values)
        {
            Append(value);
        }
    }

    public Span<ulong> GetWritableSpan(int size)
    {
        Ensure(size);
        return CastedSpan.Slice(_length, size);
    }

    public void SetLength(int length)
    {
        Ensure(_length - length);
        _length = length;
    }

    public int Length => _length;

    public void CopyTo(int offset, Span<ulong> dest)
    {
        CastedSpan.Slice(offset, dest.Length).CopyTo(dest);
    }

    public ulong this[int idx]
    {
        get => CastedSpan[idx];
        set => CastedSpan[idx] = value;
    }

    public IUnpacked Unpack()
    {
        var appendable = Create(Length);
        CopyTo(0, appendable.GetWritableSpan(Length));
        appendable.SetLength(Length);
        return appendable;
    }

    public ReadOnlySpan<ulong> Span => CastedSpan.SliceFast(0, _length);

    /// <summary>
    /// Analyze the column and pack it into a more efficient representation, this will either be a constant
    /// value, an unpacked array, or a packed array. Packed arrays use a bit of bit twiddling to efficiently
    /// store the most common patterns of ids in the system
    /// </summary>
    public IReadable Pack()
    {
        var stats = Statistics.Create(MemoryMarshal.Cast<ulong, ulong>(Span));
        return (IReadable)stats.Pack(Span);
    }

    public IEnumerator<ulong> GetEnumerator()
    {
        for (var i = 0; i < _length; i++)
        {
            yield return CastedSpan[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
