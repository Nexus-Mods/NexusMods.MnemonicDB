using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public partial class Appendable<T> : IDisposable, IAppendable<T>, IReadable<T>, ICanBePacked<T>
    where T : struct
{
    private const int DefaultSize = 16;
    private IMemoryOwner<ulong> _data;
    private int _length;

    private Appendable(IMemoryOwner<ulong> data, int length)
    {
        _data = data;
        _length = length;
    }

    public static Appendable<T> Create(int initialSize = DefaultSize)
    {
        return new Appendable<T>(MemoryPool<ulong>.Shared.Rent(DefaultSize), 0);
    }

    private Span<T> CastedSpan => MemoryMarshal.Cast<ulong, T>(_data.Memory.Span);

    public void Dispose()
    {
        _data.Dispose();
    }

    public void Append(T value)
    {
        Ensure(1);
        CastedSpan[_length] = value;
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

    public void Append(ReadOnlySpan<T> values)
    {
        Ensure(values.Length);
        values.CopyTo(CastedSpan.Slice(_length));
        _length += values.Length;
    }

    public void Append(IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            Append(value);
        }
    }

    public Span<T> GetWritableSpan(int size)
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
        var src = MemoryMarshal.Cast<T, ulong>(CastedSpan);
        src.Slice(offset, dest.Length).CopyTo(dest);
    }

    public T this[int idx] => CastedSpan[idx];

    public void CopyTo(int offset, Span<T> dest)
    {
        CastedSpan.Slice(offset, dest.Length).CopyTo(dest);
    }

    public ReadOnlySpan<T> Span => CastedSpan.Slice(0, _length);
}
