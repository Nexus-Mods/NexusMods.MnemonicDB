using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Exceptions;
using NexusMods.HyperDuck.Internals;

namespace NexusMods.HyperDuck;

public unsafe partial class Connection : IDisposable
{
    internal void* _ptr;
    private readonly IRegistry _registry;
    private readonly Database _db;

    public Connection(IRegistry registry, Database db)
    {
        _registry = registry;
        _db = db;
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

    public Task FlushAsync()
    {
        return _db.LiveQueryUpdater.Value.FlushAsync();
    }



    private class LiveQuery<T>
    {
        public required HashSet<string> Functions { get; set; }
        public required string Query { get; set; }
        public required T Output { get; set; }
    }

    
    public T Query<T>(string query) where T : class, new()
    {
        using var stmt = Prepare(query);
        using var dbResults = stmt.Execute();
        var adapter = _registry.GetAdaptor<T>(dbResults);
        var result = new T();
        adapter.Adapt(dbResults, ref result);
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
        
        if (_db.LiveQueryUpdater.IsValueCreated)
            _db.LiveQueryUpdater.Value.Dispose();
        
        Native.duckdb_disconnect(ref _ptr);
        _ptr = null;
    }

    public void Register(AScalarFunction fn)
    {
        fn.Register(this);
    }

    public void Register(ATableFunction fn)
    {
        fn.Register(this);
        _db.TableFunctions[fn.Name] = fn;
    }
}
