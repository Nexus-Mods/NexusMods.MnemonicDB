using System;
using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

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

    public ulong Length
    {
        get => NativeMethods.DataChunks.DuckDBDataChunkGetSize(_chunk);
        set => NativeMethods.DataChunks.DuckDBDataChunkSetSize(_chunk, (ulong)value);
    }
}
