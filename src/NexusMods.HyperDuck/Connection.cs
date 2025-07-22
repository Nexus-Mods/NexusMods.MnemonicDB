using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using JetBrains.Annotations;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Exceptions;
using NexusMods.HyperDuck.Internals;

namespace NexusMods.HyperDuck;

public unsafe partial struct Connection : IDisposable
{
    internal void* _ptr;
    private readonly IRegistry _registry;
    private readonly ConcurrentDictionary<ATableFunction, ulong> _functionRevisions = new();

    public Connection(IRegistry registry)
    {
        _registry = registry;
    }
    
    public void Interrupt()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        Native.duckdb_interrupt(_ptr);
    }

    protected void ReviseFunction(ATableFunction function)
    {
    }

    [MustUseReturnValue]
    public Result Query(string query)
    {
        Result result = new Result();
        if (Native.duckdb_query(_ptr, query, ref result) != State.Success)
            throw new QueryException(Marshal.PtrToStringUTF8((IntPtr)Native.duckdb_result_error(ref result)) ?? "Unknown error");
        return result;
    }

    public IDisposable ObserveQuery<T>(string query, ref T output)
    {
        var deps = GetReferencedFunctions(query);
        using var initial = Query(query);
        var adapter = _registry.GetAdaptor<T>(initial);
        adapter.Adapt(initial, ref output);

        while (true)
        {
            using var dbResults = Query(query);
            adapter.Adapt(dbResults, ref output);
        }
    }

    private class LiveQuery<T>
    {
        public required HashSet<string> Functions { get; set; }
        public required string Query { get; set; }
        public required T Output { get; set; }
    }

    
    public T Query<T>(string query) where T : class, new()
    {
        using var dbResults = Query(query);
        var adapter = _registry.GetAdaptor<T>(dbResults);
        var result = new T();
        adapter.Adapt(dbResults, ref result);
        return result;
    }

    /// <summary>
    /// Gets the JSON query plan for a specific query
    /// </summary>
    public HashSet<string> GetReferencedFunctions(string query)
    {
        var result = Query<List<(string, string)>>("EXPLAIN (FORMAT JSON) " + query);

        var plan = JsonSerializer.Deserialize<QueryPlanNode[]>(result.First().Item2)!;
        
        HashSet<string> touchedFunctions = [];
        foreach (var node in plan)
        {
            AnalyzeNode(node, touchedFunctions);
        }
        
        return touchedFunctions;

        void AnalyzeNode(QueryPlanNode node, HashSet<string> touchedFunctions)
        {
            if (node.ExtraInfo?.Function is { } functionName)
            {
                touchedFunctions.Add(functionName);
            }
            foreach (var child in node.Children)
                AnalyzeNode(child, touchedFunctions);
        }
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
    }

    public void Dispose()
    {
        if (_ptr == null) return;
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
    }
}
