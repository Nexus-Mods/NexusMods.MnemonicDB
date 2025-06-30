using System;
using System.Runtime.CompilerServices;
using DuckDB.NET.Native;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.QueryV2;

public unsafe struct Vector<THighLevel, TLowLevel> : 
    IHighLevelVector<THighLevel, Vector<THighLevel, TLowLevel>>, ILowLevelVector<TLowLevel> 
{
    /// <summary>
    /// An array of 64-bit unsinged integers representing the validity mask for the vector. If the bit
    /// for the index is not set, the value at that index is considered invalid (NULL).
    /// </summary>
    private readonly ulong* _validityMaskPtr;
    private readonly Func<TLowLevel, THighLevel> _converter;
    private readonly void* _data;

    public Vector(void* data, ulong* validityData, Func<TLowLevel, THighLevel> converter)
    {
        _data = data;
        _validityMaskPtr = validityData;
        _converter = converter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TLowLevel GetLowLevel(int index)
    {
        if (typeof(TLowLevel) == typeof(int))
            return (TLowLevel)(object)((int*)_data)[index];
        
        if (typeof(TLowLevel) == typeof(string))
            return (TLowLevel)(object)(((DuckDBStringValue*)_data)[index]).GetValue();
        
        throw new NotImplementedException($"Not implemented for {typeof(TLowLevel)}");
        
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public THighLevel ReadHighLevel(int index)
    {
        return _converter(GetLowLevel(index));
    }

    public THighLevel this[int index]
    {
        get
        {
            var isValid = IsValid(index);
            if (!isValid)
                return default!;
            return ReadHighLevel(index);
        }
    }

    public void Reset(DuckDBDataChunk chunk, int column)
    {
        throw new NotImplementedException();
    }

    public static unsafe Vector<THighLevel, TLowLevel> Create(DuckDBDataChunk chunk, int column)
    {
        var vector = NativeMethods.DataChunks.DuckDBDataChunkGetVector(chunk, column);
        var data = NativeMethods.Vectors.DuckDBVectorGetData(vector);
        var validity = NativeMethods.Vectors.DuckDBVectorGetValidity(vector);
        return new Vector<THighLevel, TLowLevel>(data, validity, x => (THighLevel)(object)(TLowLevel)x!);
    }
    
    public bool IsValid(int index)
    {
        unsafe
        {
            if (_validityMaskPtr == null) 
                return true;
            var longIndex = index / 64;
            var bitIndex = index % 64;
            var validityMask = _validityMaskPtr[longIndex];
            return (validityMask & (1UL << bitIndex)) != 0;
        }
    }
}
