using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Abstractions.PackingStrategies;

namespace NexusMods.EventSourcing.Storage.Columns;

public class UnsignedIntegerColumn<T> : IAppendableColumn<T>, IUnpackedColumn<T>, IEnumerable<T>
where T : unmanaged
{
    private uint _length;
    private T[] _data;

    public UnsignedIntegerColumn(uint initialLength = RawDataChunk.DefaultChunkSize)
    {
        _length = 0;
        _data = GC.AllocateUninitializedArray<T>((int)initialLength);
    }

    public UnsignedIntegerColumn(IColumn<T> data)
    {
        _length = (uint)data.Length;
        _data = GC.AllocateUninitializedArray<T>(data.Length);
        data.CopyTo(_data);
    }

    public T this[int index] => _data[index];

    public int Length => (int)_length;

    public void CopyTo(Span<T> destination)
    {
        _data.AsSpan()[..Length].CopyTo(destination);
    }

    /// <summary>
    /// Appends a value to the end of the column.
    /// </summary>
    public void Append(T value)
    {
        if (_length == _data.Length)
        {
            Array.Resize(ref _data, _data.Length * 2);
        }
        _data[_length++] = value;
    }

    /// <summary>
    /// Clears the column, resetting the length to 0, does not remove the data or resize
    /// the buffers
    /// </summary>
    public void Reset()
    {
        _length = 0;
    }

    public void Initialize(ReadOnlySpan<T> value)
    {
        if (_length != 0)
        {
            throw new InvalidOperationException("Column is already initialized");
        }
        if (_data.Length < value.Length)
        {
            _data = GC.AllocateUninitializedArray<T>(value.Length);
        }
        value.CopyTo(_data);
        _length = (uint)value.Length;
    }

    public void Initialize(IEnumerable<T> value)
    {
        _length = 0;
        foreach (var v in value)
        {
            Append(v);
        }
    }

    public IColumn<T> Pack()
    {
        return UnsignedInteger.Pack(this);
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotSupportedException("Columns must be packed before writing to a buffer.");
    }


    public ReadOnlySpan<T> Data => _data.AsSpan(0, (int)_length);
    public ReadOnlyMemory<T> Memory => _data.AsMemory(0, (int)_length);

    public void Shuffle(int[] pidxs)
    {
        var newData = GC.AllocateArray<T>((int)_length);
        for (var i = 0; i < _length; i++)
        {
            newData[i] = _data[pidxs[i]];
        }
        _data = newData;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _length; i++)
        {
            yield return _data[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static UnsignedIntegerColumn<T> UnpackFrom(IColumn<T> fromValues)
    {
        return new UnsignedIntegerColumn<T>(fromValues);
    }
}
