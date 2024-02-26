using System;
using NexusMods.EventSourcing.Storage.Abstractions.PackingStrategies;

namespace NexusMods.EventSourcing.Storage.Abstractions.Columns;

/// <summary>
/// A column of data that is managed, and of a fixed struct size
/// </summary>
/// <typeparam name="T"></typeparam>
public class ManagedAppendableColumn<T> : IUnpackedColumn<T>, IAppendableColumn<T>
{
    private T[] _data;
    private int _count;

    /// <summary>
    /// Copies the span into a new managed array, and creates a new ManagedColumn.
    /// </summary>
    /// <param name="data"></param>
    public ManagedAppendableColumn(ReadOnlySpan<T> data)
    {
        _data = GC.AllocateUninitializedArray<T>(data.Length);
        data.CopyTo(_data);
        _count = data.Length;
    }

    public ManagedAppendableColumn(int initalSize = RawDataChunk.DefaultChunkSize)
    {
        _data = GC.AllocateUninitializedArray<T>(initalSize);
        _count = 0;
    }

    public T this[int index] => Data[index];

    public int Length => _count;

    public void CopyTo(Span<T> destination)
    {
        Data.CopyTo(destination);
    }

    public ReadOnlySpan<T> Data => _data.AsSpan(0, _count);

    public void Append(T value)
    {
        if (_count == _data.Length)
        {
            Array.Resize(ref _data, _data.Length * 2);
        }
        _data[_count++] = value;
    }

    public void Initialize(ReadOnlySpan<T> value)
    {
        if (_count != 0)
        {
            throw new InvalidOperationException("Column is already initialized");
        }
        if (_data.Length < value.Length)
        {
            _data = GC.AllocateUninitializedArray<T>(value.Length);
        }
        value.CopyTo(_data);
        _count = value.Length;
    }

    public IPackedColumn<T> Pack()
    {
        if (typeof(T) == typeof(ulong))
        {
            return (IPackedColumn<T>)UnsignedInteger.Pack((IUnpackedColumn<ulong>)this);
        }

        throw new NotImplementedException();
    }

    public void Swap(int idx1, int idx2)
    {
        (_data[idx1], _data[idx2]) = (_data[idx2], _data[idx1]);
    }
}
