using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Exceptions;
using NexusMods.HyperDuck.Internals;

namespace NexusMods.HyperDuck;

public unsafe partial class Database : IDisposable
{
    private void *_ptr;
    private readonly IRegistry _registry;
    private readonly ConcurrentBag<PooledConnection> _connections = [];
    private readonly ConcurrentBag<PooledConnection> _allConnections = [];
    
    internal readonly Lazy<LiveQueryUpdater> LiveQueryUpdater =  new(static () => new LiveQueryUpdater());
    internal readonly ConcurrentDictionary<string, ATableFunction> TableFunctions = new();
    private readonly TimeSpan _delay;

    public Database(IRegistry registry)
    {
        _registry = registry;
    }

    public IRegistry Registry => _registry;


    [MustDisposeResource]
    public static Database Open(string path, IRegistry registry)
    {
        var state = new Database(registry);
        var result = Native.duckdb_open(path, ref state._ptr);
        if (result != State.Success)
        {
            throw new OpenDatabaseException();
        }
        return state;
    }

    /// <summary>
    /// Open an in-memory database.
    /// </summary>
    public static Database OpenInMemory(IRegistry registry)
    {
        return Open(":memory:", registry);
    }

    [MustDisposeResource]
    public PooledConnection Connect()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        if (_connections.TryTake(out var pooledConnection))
            return pooledConnection;
        
        var connection = new Connection(_registry, this);
        if (Native.duckdb_connect( this._ptr, ref connection._ptr) != State.Success)
        {
            throw new ConnectException();
        }
        pooledConnection = new PooledConnection(connection, this);
        _allConnections.Add(pooledConnection);
        return pooledConnection;
    }


    private static partial class Native
    {

        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial State duckdb_open(string path, ref void* db);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_close(ref void* db);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial State duckdb_connect(void* db, ref void* connection);
    }

    public void Dispose()
    {
        if (_ptr == null)
        {
            return;
        }

        while (_connections.TryTake(out var connection))
        {
            connection.Destory();
        }
        
        Native.duckdb_close(ref _ptr);
        _ptr = null;
    }
    
    /// <summary>
    /// Executes the query and returns the result, adapting it to the given return type. 
    /// </summary>
    public TResult Query<TResult>(CompiledQuery<TResult> query) where TResult : new()
    {
        var returnValue = new TResult();
        QueryInto(query, ref returnValue);
        return returnValue;
    }
    
    /// <summary>
    /// Same as Query, except the results are adapted into a provided preexisting result value
    /// </summary>
    public void QueryInto<TResult>(CompiledQuery<TResult> query, ref TResult returnValue)
    {
        using var conn = Connect();
        var prepared = conn.Prepare(query);
        using var result = prepared.Execute();
        var adaptor = query.Adaptor(result, Registry);
        adaptor.Adapt(result, ref returnValue);
    }
    
    /// <summary>
    /// Executes the query and returns the result, adapting it to the given return type. 
    /// </summary>
    public TResult Query<TResult, TArg1>(CompiledQuery<TResult, TArg1> query, TArg1 arg1) where TResult : new()
    {
        var returnValue = new TResult();
        QueryInto(query, arg1, ref returnValue);
        return returnValue;
    }
    
    /// <summary>
    /// Same as Query, except the results are adapted into a provided preexisting result value
    /// </summary>
    public void QueryInto<TResult, TArg1>(CompiledQuery<TResult, TArg1> query, TArg1 arg1, ref TResult returnValue)
    {
        using var conn = Connect();
        var prepared = conn.Prepare(query);
        prepared.Bind(1, arg1);
        using var result = prepared.Execute();
        var adaptor = query.Adaptor(result, Registry);
        adaptor.Adapt(result, ref returnValue);
    }

    public Task FlushQueries()
    {
        return LiveQueryUpdater.Value.FlushAsync();
    }

    internal void Return(PooledConnection pooledConnection)
    {
        _connections.Add(pooledConnection);
    }

    public void Register(ATableFunction aTableFunction)
    {
        using var conn = Connect();
        aTableFunction.Register(conn.Connection);
        TableFunctions[aTableFunction.Name.ToUpper()] = aTableFunction;
    }
    
    public void Register(AScalarFunction aTableFunction)
    {
        using var conn = Connect();
        aTableFunction.Register(conn.Connection);
    }
    
    /// <summary>
    /// Gets the JSON query plan for a specific query
    /// </summary>
    public HashSet<ATableFunction> GetReferencedFunctions(string query)
    {
        var result = Query(HyperDuck.Query.Compile<List<(string, string)>>("EXPLAIN (FORMAT JSON) " + query));

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
            if (TableFunctions.TryGetValue(functionName, out var function))
                touchedFunctions.Add(function);
        }
        foreach (var child in node.Children)
            AnalyzeNode(child, touchedFunctions);
    }
    
    public IDisposable ObserveQuery<T>(CompiledQuery<T> query, ref T output)
    {
        var deps = GetReferencedFunctions(query.Sql);
        QueryInto(query, ref output);

        var live = new Internals.LiveQuery<T>
        {
            DependsOn = deps.ToArray(),
            Database = this,
            Query = query,
            Output = output,
            Updater = LiveQueryUpdater.Value
        };

        LiveQueryUpdater.Value.Add(live);
        return live;
    }
}
