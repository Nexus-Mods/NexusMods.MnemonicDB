using System;
using System.Runtime.CompilerServices;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public class Constant<T> : IPacked<T>
    where T : struct
{
    private readonly ulong _value;
    private readonly int _length;

    public Constant(ulong value, int length)
    {
        _value = value;
        _length = length;
    }

    public int Length => _length;

    public void CopyTo(int offset, Span<ulong> dest)
    {
        dest.Slice(0, _length).Fill(_value);
    }

    public T this[int idx] => Unsafe.BitCast<ulong, T>(_value);

    public void CopyTo(int offset, Span<T> dest)
    {
        dest.Slice(0, _length).Fill(Unsafe.BitCast<ulong, T>(_value));
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
