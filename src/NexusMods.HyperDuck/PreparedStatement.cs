using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;

namespace NexusMods.HyperDuck;

public unsafe partial struct PreparedStatement : IDisposable
{
    public void* _ptr;
    private readonly Connection _connection;
    private readonly IRegistry _registry;

    public PreparedStatement(void* ptr, IRegistry registry, Connection connection)
    {
        _ptr = ptr;
        _connection = connection;
        _registry = registry;
    }

    public void Bind<T>(int idx, T value)
    {
        var converter = _registry.GetBindingConverter<T>(value);
        converter.Bind(this, idx, value);
    }

    public static partial class Native
    {
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_destroy_prepare(ref void* stmt);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_execute_prepared(void* stmt, ref Result result);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_bind_int32(void* stmt, int idx, int val);
        
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
            var error = Marshal.PtrToStringUTF8((nint)Connection.Native.duckdb_result_error(ref resultPtr));
            resultPtr.Dispose();
            throw new InvalidOperationException("Failed to execute prepared statement: " + error);
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
