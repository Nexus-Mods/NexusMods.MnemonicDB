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

public unsafe partial struct Connection : IDisposable
{
    internal void* _ptr;
    private readonly IRegistry _registry;
    private readonly Lazy<LiveQueryUpdater> _liveQueryUpdater =  new(static () => new LiveQueryUpdater());
    private readonly ConcurrentDictionary<string, ATableFunction> _tableFunctions = new();
    private readonly TimeSpan _delay;

    public Connection(IRegistry registry)
    {
        _registry = registry;
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
        return _liveQueryUpdater.Value.FlushAsync();
    }

    public IDisposable ObserveQuery<T>(string query, ref T output)
    {
        var deps = GetReferencedFunctions(query);
        var statement = Prepare(query);
        using var initial = statement.Execute();
        var adapter = _registry.GetAdaptor<T>(initial);
        adapter.Adapt(initial, ref output);

        var live = new Internals.LiveQuery<T>
        {
            DependsOn = deps.ToArray(),
            Connection = this,
            Statement = statement,
            Output = output,
            ResultAdaptor = adapter,
            Updater = _liveQueryUpdater.Value
        };

        _liveQueryUpdater.Value.Add(live);
        return live;
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
            throw new QueryException("Cannot prepare statement.");
        
        return new PreparedStatement(stmt, this);
    }

    /// <summary>
    /// Gets the JSON query plan for a specific query
    /// </summary>
    public HashSet<ATableFunction> GetReferencedFunctions(string query)
    {
        var result = Query<List<(string, string)>>("EXPLAIN (FORMAT JSON) " + query);

        var plan = JsonSerializer.Deserialize<QueryPlanNode[]>(result.First().Item2)!;
        
        HashSet<ATableFunction> touchedFunctions = [];
        foreach (var node in plan)
        {
            AnalyzeNode(node, touchedFunctions);
        }
        
        return touchedFunctions;
    }
    
    private void AnalyzeNode(QueryPlanNode node, HashSet<ATableFunction> touchedFunctions)
    {
        if (node.ExtraInfo?.Function is { } functionName)
        {
            if (_tableFunctions.TryGetValue(functionName, out var function))
                touchedFunctions.Add(function);
        }
        foreach (var child in node.Children)
            AnalyzeNode(child, touchedFunctions);
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
    }

    public void Dispose()
    {
        if (_ptr == null) return;
        
        if (_liveQueryUpdater.IsValueCreated)
            _liveQueryUpdater.Value.Dispose();
        
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
        _tableFunctions[fn.Name] = fn;
    }
}
