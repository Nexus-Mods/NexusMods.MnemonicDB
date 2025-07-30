using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.HyperDuck;

public unsafe partial struct ReadOnlyChunk : IDisposable
{
    private void* _ptr;

    internal ReadOnlyChunk(void* ptr)
    {
        _ptr = ptr;
    }
    
    public ulong Size
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_ptr);
            return Native.duckdb_data_chunk_get_size(_ptr);
        }
    }
    
    public ulong ColumnCount
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_ptr);
            return Native.duckdb_data_chunk_get_column_count(this);
        }
    }
    
    /// <summary>
    /// Checks if the ReadOnlyChunk is valid, when iterating over a result set,
    /// the last chunk will be invalid, and so this can be used to determine
    /// when to stop iterating.
    /// </summary>
    public bool IsValid => _ptr != null;
    
    /// <summary>
    /// Gets the vector for the specified column index.
    /// </summary>
    public ReadOnlyVector this[ulong index]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_ptr);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, ColumnCount, "Column index is out of range.");
            return new ReadOnlyVector(Native.duckdb_data_chunk_get_vector(_ptr, index), Size);
        }
    }

    public ReadOnlyVector GetVector(ulong index)
    {
        return this[index];
    }


    internal static partial class Native
    {
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong duckdb_data_chunk_get_size(void* chunk);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong duckdb_data_chunk_get_column_count(ReadOnlyChunk chunk);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_destroy_data_chunk(ref ReadOnlyChunk ptr);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_data_chunk_get_vector(void* chunk, ulong index);
    }

    public void Dispose()
    {
        if (_ptr == null) return;
        Native.duckdb_destroy_data_chunk(ref this);
        _ptr = null;
    }
}