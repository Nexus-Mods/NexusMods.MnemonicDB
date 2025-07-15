using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

public unsafe ref partial struct WritableVector
{
    private readonly void* _ptr;
    private readonly ulong _rowCount;
    private Span<byte> _data = Span<byte>.Empty;

    internal WritableVector(void* ptr, ulong rowCount)
    {
        _ptr = ptr;
        _rowCount = rowCount;
    }
    
    /// <summary>
    /// Returns true if this vector is valid (has a non-null ptr)
    /// </summary>
    public bool IsValid => _ptr != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Span<byte> GetData()
    {
        if (_ptr == null)
            return Span<byte>.Empty;
        if (!_data.IsEmpty) 
            return _data;
        
        var dataPtr = ReadOnlyVector.Native.duckdb_vector_get_data(_ptr);
        // Assuming the size of the vector can be determined by some means, e.g., a property or method
        using var type = GetColumnType();
        var span = type.SizeInVector;
        _data = new Span<byte>(dataPtr, span * (int)_rowCount);
        return _data;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Span<T> GetData<T>() 
        where T : unmanaged
    {
        if (_ptr == null)
            return Span<T>.Empty;
        var data = GetData();
        return MemoryMarshal.Cast<byte, T>(data);
    }

    public void WriteUtf8(ulong idx, ReadOnlySpan<byte> data)
    {
        fixed (void* ptr = data)
        {
            Native.duckdb_vector_assign_string_element_len(_ptr, idx, ptr, (ulong)data.Length);
        }
    }
    
    public WritableVector GetStructChild(ulong idx)
    {
        return new WritableVector(ReadOnlyVector.Native.duckdb_struct_vector_get_child(_ptr, idx), _rowCount);
    }

    public WritableValidityMask GetValidityMask()
    {
        ReadOnlyVector.Native.duckdb_vector_ensure_validity_writable(_ptr);
        return new WritableValidityMask(ReadOnlyVector.Native.duckdb_vector_get_validity(_ptr), _rowCount);
    }

    [MustDisposeResource]
    public LogicalType GetColumnType()
    {
        ArgumentNullException.ThrowIfNull(_ptr);

        // Assuming duckdb_vector_get_column_type returns a LogicalType
        var type = ReadOnlyVector.Native.duckdb_vector_get_column_type(_ptr);
        return type;
    }

    internal static partial class Native
    {
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_vector_assign_string_element_len(void* vector, ulong offset, void* str, ulong length);
    }

}
