using System;
using DuckDB.NET.Native;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.QueryV2;


public unsafe struct VectorValueWriter
{
    private readonly IntPtr _vector;
    private readonly void* _data;
    private readonly int _span;
    private readonly DuckDBType _type;

    public VectorValueWriter(IntPtr vector, void* data, int span, DuckDBType type)
    {
        _vector = vector;
        _type = type;
        _data = data;
        _span = span;
    }

    public void Write(int row, ReadOnlySpan<byte> value)
    {
        if (_span == -1)
        {
            WriteSlow(row, value);
            return;
        }
        var target = (byte*)_data + row * _span;
        value.CopyTo(new Span<byte>(target, _span));
    }
    
    public void WriteValue<T>(int row, T value)
        where T : unmanaged
    {
        if (_span != sizeof(T))
        {
            throw new InvalidOperationException($"Cannot write value of type {typeof(T)} to vector with span {_span}");
        }

        var target = (T*)((byte*)_data + row * _span);
        *target = value;
    }

    private void WriteSlow(int row, ReadOnlySpan<byte> value)
    { 
        if (_type == DuckDBType.Varchar)
        {
            fixed (byte* valuePtr = value)
            {
                NativeMethods.Vectors.DuckDBVectorAssignStringElementLength(_vector, (ulong)row, valuePtr, value.Length);
                return;
            }
        }

        throw new NotImplementedException($"Now way to write {_type} yet");
    }
}
public ref struct DuckDBChunkWriter
{
    private readonly DuckDBDataChunk _chunk;
    private readonly int[] _bindData;

    internal DuckDBChunkWriter(DuckDBDataChunk chunk, int[] bindData)
    {
        _chunk = chunk;
        _bindData = bindData;
    }
    
    public unsafe Span<T> GetVector<T>(int columnIndex)
        where T : unmanaged
    {
        var remappedIndex = _bindData[columnIndex];
        if (remappedIndex == -1)
        {
            return Span<T>.Empty;
        }
        
        var vector = NativeMethods.DataChunks.DuckDBDataChunkGetVector(_chunk, columnIndex);
        if (vector == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Column index {columnIndex} is out of bounds.");
        }

        var data = NativeMethods.Vectors.DuckDBVectorGetData(vector);
        return new Span<T>(data, (int)DuckDBExtensions.VectorSize);
    }

    public unsafe Span<byte> GetVector(int columnIndex, ValueTag type)
    {
        var remappedIndex = _bindData[columnIndex];
        if (remappedIndex == -1)
        {
            return Span<byte>.Empty;
        }

        var vector = NativeMethods.DataChunks.DuckDBDataChunkGetVector(_chunk, columnIndex);
        
        if (vector == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Column index {columnIndex} is out of bounds.");
        }

        var data = NativeMethods.Vectors.DuckDBVectorGetData(vector);
        return new Span<byte>(data, (int)DuckDBExtensions.VectorSize * GetSize(type));
    }

    private int GetSize(ValueTag type)
    {
        return type switch
        {
            ValueTag.Reference => sizeof(ulong),
            ValueTag.Utf8 => -1,
            ValueTag.Null => 1, // Nulls are represented as a single byte
            ValueTag.UInt64 => sizeof(ulong),
            _ => throw new NotSupportedException($"ValueTag {type} is not supported by DuckDB.")
        };
    }
    

    public ulong Length
    {
        get => NativeMethods.DataChunks.DuckDBDataChunkGetSize(_chunk);
        set => NativeMethods.DataChunks.DuckDBDataChunkSetSize(_chunk, (ulong)value);
    }

    public unsafe VectorValueWriter GetWriter(int i, ValueTag type)
    {
        var vector = NativeMethods.DataChunks.DuckDBDataChunkGetVector(_chunk, i);
        var vectorData = NativeMethods.Vectors.DuckDBVectorGetData(vector);
        var size = GetSize(type);
        return new VectorValueWriter(vector, vectorData, size, type.ToDuckDBType());
    }
}
