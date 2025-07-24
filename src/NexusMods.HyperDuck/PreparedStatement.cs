using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

public unsafe partial struct PreparedStatement : IDisposable
{
    private void* _ptr;
    private readonly Connection _connection;

    public PreparedStatement(void* ptr, Connection connection)
    {
        _ptr = ptr;
        _connection = connection;
    }

    public void Bind<T>(int idx, T value)
    {
        switch (value)
        {
            case ulong v:
                Native.duckdb_bind_uint64(_ptr, idx, v);
                break;
            default:
                throw new NotImplementedException("No way to bind {value.GetType().Name}");
        }
    }

    internal static partial class Native
    {
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_destroy_prepare(ref void* stmt);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_execute_prepared(void* stmt, ref Result result);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_bind_uint64(void* stmt, int idx, ulong val);
    }

    [MustDisposeResource]
    public Result Execute()
    {
        Result resultPtr = new Result();
        if (Native.duckdb_execute_prepared(_ptr, ref resultPtr) != State.Success)
        {
            resultPtr.Dispose();
            throw new InvalidOperationException("Failed to execute prepared statement.");
        }
        return resultPtr;
    }

    public void Dispose()
    {
        if (_ptr == null) 
            return;
        Native.duckdb_destroy_prepare(ref _ptr);
        _ptr = null;
    }
}
