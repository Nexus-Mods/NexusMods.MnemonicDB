using System;
using System.Runtime.InteropServices;
using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

/// <summary>
/// High-performance zero-allocation bindings for DuckDB
/// </summary>
public static class HighPerfBindings
{
    private const string DuckDbLibrary = "duckdb";
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_bind_get_parameter")]
    public static extern DuckDBValue DuckDBBindGetParameter(IntPtr info, ulong index);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_column_logical_type")]
    public static extern DuckDBLogicalType DuckDBColumnLogicalType([In, Out] ref DuckDBResult result, long col);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_create_logical_type")]
    public static extern DuckDBLogicalType DuckDBCreateLogicalType(DuckDBType type);

    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_bind_add_result_column")]
    public static extern void DuckDBBindAddResultColumn(IntPtr info, SafeUnmanagedMemoryHandle name, DuckDBLogicalType type);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_table_function_supports_projection_pushdown")]
    public static extern unsafe void DuckDBTableFunctionSupportsProjectionPushdown(IntPtr tableFunction, bool supportsProjectionPushdown);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_init_get_column_count")]
    public static extern unsafe uint DuckDBInitGetColumnCount(IntPtr initInfo);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_init_get_column_index")]
    public static extern unsafe int DuckDBInitGetColumnIndex(IntPtr initInfo, uint index);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_init_get_bind_data")]
    public static extern unsafe IntPtr DuckDBInitGetBindInfo(IntPtr initInfo);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_init_get_extra_info")]
    public static extern unsafe IntPtr DuckDBInitGetExtraInfo(IntPtr initInfo);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_init_set_init_data")]
    public static extern unsafe void DuckDBInitSetInitData(IntPtr initInfo, IntPtr initData, delegate* unmanaged[Cdecl]<IntPtr, void> destroy);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_function_get_init_data")]
    public static extern IntPtr DuckDBFunctionGetInitData(IntPtr info);
    
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_create_struct_type")]
    public static extern DuckDBLogicalType DuckDBCreateStructType(DuckDBLogicalType[] types, string[] names, int memberCount);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_column_logical_type")]
    public static extern DuckDBLogicalType DuckDbColumnLogicalType([In, Out] ref DuckDBResult result, long col);
    
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_get_type_id")]
    public static extern DuckDBType DuckDBGetTypeId(DuckDBLogicalType type);
    
        
    [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_struct_type_child_count")]
    public static extern int DuckDBStructTypeChildCount(DuckDBLogicalType type);


}
