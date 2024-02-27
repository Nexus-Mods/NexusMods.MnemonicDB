using System;
using System.Buffers;
using System.Numerics;
using NexusMods.EventSourcing.Storage.Abstractions.PackingStrategies;

namespace NexusMods.EventSourcing.Storage.Abstractions.Columns;

public class UnsignedIntegerColumn<T> : IAppendableColumn<T>, IUnpackedColumn<T>
where T : unmanaged
{
    private uint _length;
    private T[] _data;

    public UnsignedIntegerColumn(uint initialLength = RawDataChunk.DefaultChunkSize)
    {
        _length = 0;
        _data = GC.AllocateUninitializedArray<T>((int)initialLength);
    }

    public T this[int index] => _data[index];

    public int Length => (int)_length;

    public void CopyTo(Span<T> destination)
    {
        _data.AsSpan().CopyTo(destination);
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

    public IColumn<T> Pack()
    {
        return UnsignedInteger.Pack(this);
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotSupportedException("Columns must be packed before writing to a buffer.");
    }

    public void Swap(int idx1, int idx2)
    {
        (_data[idx1], _data[idx2]) = (_data[idx2], _data[idx1]);
    }

    public ReadOnlySpan<T> Data => _data.AsSpan(0, (int)_length);
    public ReadOnlyMemory<T> Memory => _data.AsMemory(0, (int)_length);
}
