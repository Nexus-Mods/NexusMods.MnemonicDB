using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.HyperDuck;

public unsafe partial struct WritableChunk
{
    private void* _ptr;
    
    public WritableChunk(void* ptr)
    {
        _ptr = ptr;
    }

    public ulong Size
    {
        get => ReadOnlyChunk.Native.duckdb_data_chunk_get_size(_ptr);
        set => Native.duckdb_data_chunk_set_size(_ptr, value);
    }

    public WritableVector this[ulong idx]
    {
        get => new(ReadOnlyChunk.Native.duckdb_data_chunk_get_vector(_ptr, idx), GlobalConstants.DefaultVectorSize);
    }

    public static partial class Native
    {
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_data_chunk_set_size(void* chunk, ulong size);

        
    }
}