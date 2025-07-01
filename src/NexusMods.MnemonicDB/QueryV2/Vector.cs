using System;
using System.Runtime.CompilerServices;
using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

/// <summary>
/// DuckDB manipulates data as chunks of vectors. Each vector is roughly 2048 elements long, and contains the
/// data for a single column of data. 
/// </summary>
public unsafe struct Vector<TLowLevel> : IVector<TLowLevel, Vector<TLowLevel>> 
{
    /// <summary>
    /// An array of 64-bit unsinged integers representing the validity mask for the vector. If the bit
    /// for the index is not set, the value at that index is considered invalid (NULL).
    /// </summary>
    private readonly ulong* _validityMaskPtr;
    private readonly void* _data;

    public Vector(void* data, ulong* validityData)
    {
        _data = data;
        _validityMaskPtr = validityData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TLowLevel GetLowLevel(int index)
    {
        if (typeof(TLowLevel) == typeof(int))
            return (TLowLevel)(object)((int*)_data)[index];
        
        if (typeof(TLowLevel) == typeof(ulong))
            return (TLowLevel)(object)((ulong*)_data)[index];
        
        if (typeof(TLowLevel) == typeof(string))
            return (TLowLevel)(object)(((DuckDBStringValue*)_data)[index]).GetValue();
        
        throw new NotImplementedException($"Not implemented for {typeof(TLowLevel)}");
        
    }

    public void Reset(DuckDBDataChunk chunk, int column)
    {
        throw new NotImplementedException();
    }

    public static unsafe Vector<TLowLevel> Create(DuckDBDataChunk chunk, int column)
    {
        var vector = NativeMethods.DataChunks.DuckDBDataChunkGetVector(chunk, column);
        var data = NativeMethods.Vectors.DuckDBVectorGetData(vector);
        var validity = NativeMethods.Vectors.DuckDBVectorGetValidity(vector);
        return new Vector<TLowLevel>(data, validity);
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
