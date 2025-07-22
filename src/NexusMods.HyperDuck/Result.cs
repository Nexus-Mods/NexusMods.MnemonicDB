using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

internal unsafe partial struct duckdb_column
{
    // Deprecated, use `duckdb_column_data`.
    void *deprecated_data;
    // Deprecated, use `duckdb_nullmask_data`.
    bool *deprecated_nullmask;
    // Deprecated, use `duckdb_column_type`.
    ulong deprecated_type;
    // Deprecated, use `duckdb_column_name`.
    char *deprecated_name;
    void *internal_data;
}


public unsafe partial struct Result : IDisposable
{
    // Deprecated, use `duckdb_column_count`.
    [Obsolete]
    ulong deprecated_column_count;
    // Deprecated, use `duckdb_row_count`.
    [Obsolete]
    ulong deprecated_row_count;
    // Deprecated, use `duckdb_rows_changed`.
    [Obsolete]
    ulong deprecated_rows_changed;
    // Deprecated, use `duckdb_column_*`-family of functions.
    [Obsolete]
    duckdb_column *deprecated_columns;
    // Deprecated, use `duckdb_result_error`.
    char *deprecated_error_message;
    void *internal_data;

    
    public ulong ColumnCount => Native.duckdb_column_count(ref this);
    
    public ulong RowCount => Native.duckdb_row_count(ref this);

    /*
    public IEnumerable<ColumnInfo> Columns
    {
        get
        {
            for (ulong i = 0; i < ColumnCount; i++)
            {
                yield return new ColumnInfo(this, i);
            }
        }
    }*/
    
    public ColumnInfo GetColumnInfo(ulong index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, ColumnCount, "Column index is out of range.");
        return new ColumnInfo(this, index);
    }
    
    [MustDisposeResource]
    public ReadOnlyChunk FetchChunk()
    {
        return Native.duckdb_fetch_chunk(this);
    }

    public struct ColumnInfo
    {
        private Result _result;
        private readonly ulong _index;
        
        internal ColumnInfo(Result result, ulong index)
        {
            _result = result;
            _index = index;
        }
        
        public string Name => Marshal.PtrToStringUTF8((IntPtr)Native.duckdb_column_name(ref _result, _index))!;
        public DuckDbType Type => Native.duckdb_column_type(ref _result, _index);

        [MustDisposeResource]
        public LogicalType GetLogicalType()
        {
            return new LogicalType(Native.duckdb_column_logical_type(ref _result, _index));
        }
    }

    private static partial class Native
    {
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_destroy_result(ref Result result);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong duckdb_column_count(ref Result result);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong duckdb_row_count(ref Result result);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial char* duckdb_column_name(ref Result result, ulong column_index);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial DuckDbType duckdb_column_type(ref Result result, ulong column_index);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_column_logical_type(ref Result result, ulong column_index);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ReadOnlyChunk duckdb_fetch_chunk(Result result);
    }

    public void Dispose()
    {
        Native.duckdb_destroy_result(ref this);
    }
}
