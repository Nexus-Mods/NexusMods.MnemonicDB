using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.HyperDuck.Exceptions;

namespace NexusMods.HyperDuck;

public unsafe partial class Connection : IDisposable
{
    internal void* _ptr;

    public Connection(void *ptr)
    {
        _ptr = ptr;
    }
    
    public void Interrupt()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        Native.duckdb_interrupt(_ptr);
    }


    [MustUseReturnValue]
    public Result Query(string query)
    {
        Result result = new Result();
        if (Native.duckdb_query(_ptr, query, ref result) != State.Success)
            throw new QueryException(Marshal.PtrToStringUTF8((IntPtr)Native.duckdb_result_error(ref result)) ?? "Unknown error");
        return result;
    }
    
    [MustDisposeResource]
    public PreparedStatement Prepare(string query)
    {
        void* stmt = default!;
        if (Native.duckdb_prepare(_ptr, query, ref stmt) != State.Success)
        {
            var error = Marshal.PtrToStringUTF8((IntPtr)Native.duckdb_prepare_error(stmt));
            throw new QueryException("Cannot prepare statement: " + error);
        }

        return new PreparedStatement(stmt, this);
    }
    
    internal static partial class Native
    {
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_disconnect(ref void* connection);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_interrupt(void* connection);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_query(void* connection, string query, ref Result result);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_result_error(ref Result result);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_prepare(void* connection, string query, ref void* statement);

        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_prepare_error(void* statement);
    }

    public void Dispose()
    {
        if (_ptr == null) return;
        
        
        Native.duckdb_disconnect(ref _ptr);
        _ptr = null;
    }
}
