using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

public unsafe partial struct ReadOnlyVector
{
    private void* _ptr;
    private ulong _rowCount;
    
    internal ReadOnlyVector(void* ptr, ulong rowCount)
    {
        _ptr = ptr;
        _rowCount = rowCount;
    }
    
    public int Length => (int)_rowCount;

    /// <summary>
    /// When true, the data will be truncated to the valid size of the vector (number of elements).
    /// </summary>
    public ReadOnlySpan<byte> GetData()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        
        // Assuming duckdb_vector_get_data returns a pointer to the data
        void* dataPtr = Native.duckdb_vector_get_data(_ptr);
        if (dataPtr == null)
            throw new InvalidOperationException("Failed to get vector data.");

        // Assuming the size of the vector can be determined by some means, e.g., a property or method
        using var type = GetColumnType();
        var span = type.SizeInVector;
        return new ReadOnlySpan<byte>(dataPtr, span * (int)_rowCount);
    }

    /// <summary>
    /// Slice the vector from the given input list entry 
    /// </summary>
    public ReadOnlySpan<T> Slice<T>(ListEntry entry) 
        where T : unmanaged
    {
        return GetData<T>().Slice((int)entry.Offset, (int)entry.Length);
    }

    /// <summary>
    /// Returns the data as a span of the specified type, a check is not made to ensure that the type matches the vector's data type.
    /// </summary>
    public ReadOnlySpan<T> GetData<T>() where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        
        var data = GetData();
        return MemoryMarshal.Cast<byte, T>(data);
    }
    
    /// <summary>
    /// Returns true if the value at the given row is null
    /// </summary>
    public readonly bool IsNull(ulong rowIndex)
    {
        var mask = GetValidityMask();
        return !mask.IsValid(rowIndex);
    }
    
    [MustDisposeResource]
    public LogicalType GetColumnType()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        
        // Assuming duckdb_vector_get_column_type returns a LogicalType
        var type = Native.duckdb_vector_get_column_type(_ptr);
        return type;
    }
    
    public ReadOnlyValidityMask GetValidityMask()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        
        // Assuming duckdb_vector_get_validity returns a pointer to the validity mask
        ulong* validityPtr = Native.duckdb_vector_get_validity(_ptr);
        if (validityPtr == null)
            throw new InvalidOperationException("Failed to get vector validity mask.");

        return new ReadOnlyValidityMask(validityPtr, _rowCount);
    }

    public ReadOnlyVector GetListChild()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        
        // Assuming duckdb_list_vector_get_child returns a pointer to the child vector
        void* childPtr = Native.duckdb_list_vector_get_child(_ptr);
        if (childPtr == null)
            throw new InvalidOperationException("Failed to get list vector child.");
        var childSize = Native.duckdb_list_vector_get_size(_ptr);
        return new ReadOnlyVector(childPtr, childSize);
    }

    public ReadOnlyVector GetStructChild(ulong idx)
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        
        // Assuming duckdb_struct_vector_get_child returns a pointer to the child vector
        void* childPtr = Native.duckdb_struct_vector_get_child(_ptr, idx);
        if (childPtr == null)
            throw new InvalidOperationException("Failed to get struct vector child.");
        return new ReadOnlyVector(childPtr, _rowCount);
    }

    public static partial class Native
    {

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_vector_get_data(void* vector);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial LogicalType duckdb_vector_get_column_type(void* vector);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong* duckdb_vector_get_validity(void* vector);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_vector_ensure_validity_writable(void* vector);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_list_vector_get_child(void* vector);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong duckdb_list_vector_get_size(void* vector);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_struct_vector_get_child(void* vector, ulong idx);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong duckdb_vector_size();
    }
}
