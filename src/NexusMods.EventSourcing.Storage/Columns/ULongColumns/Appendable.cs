using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// An appendable column of ulong values. This stores values as a auto-expanding array of ulong values.
/// Backed by the shared memory pool
/// </summary>
/// <typeparam name="T"></typeparam>
public class Appendable : IDisposable, IAppendable, IReadable, IUnpacked
{
    private const int DefaultSize = 16;
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

    public ulong this[int idx] => CastedSpan[idx];

    public ReadOnlySpan<ulong> Span => CastedSpan.SliceFast(0, _length);
}
