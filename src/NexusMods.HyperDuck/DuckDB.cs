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
using NexusMods.HyperDuck.Adaptor.Impls;
using NexusMods.HyperDuck.Exceptions;
using NexusMods.HyperDuck.Internals;

namespace NexusMods.HyperDuck;

public class DuckDB : IAsyncDisposable, IQueryMixin
{

    private Database _db;
    private readonly IRegistry _registry;
    private readonly ConcurrentBag<PooledConnection> _connections = [];
    private readonly ConcurrentBag<PooledConnection> _allConnections = [];
    private uint _nextGlobalId = 0;
    private readonly ConcurrentDictionary<object, ushort> _globalIds = new();
    private readonly ConcurrentDictionary<ushort, object> _globalObjects = new();
    
    internal readonly Lazy<LiveQueryUpdater> LiveQueryUpdater =  new(static () => new LiveQueryUpdater());
    internal readonly ConcurrentDictionary<string, ATableFunction> TableFunctions = new();
    private readonly TimeSpan _delay;
    private bool _disposed;

    private DuckDB(IRegistry registry)
    {
        _disposed = false;
        _registry = registry;
        _db = Database.OpenInMemory();
    }

    public static DuckDB Open()
    {
        return new DuckDB(new Registry([], [], [], []));
    }

    public static DuckDB Open(IRegistry registry)
    {
        return new DuckDB(registry);
    }

    public IRegistry Registry => _registry;



    [MustDisposeResource]
    public PooledConnection Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_connections.TryTake(out var pooledConnection))
            return pooledConnection;
        
        var connection = _db.Connect(this);
        pooledConnection = new PooledConnection(connection, this);
        _allConnections.Add(pooledConnection);
        return pooledConnection;
    }




    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        if (LiveQueryUpdater.IsValueCreated)
            await LiveQueryUpdater.Value.DisposeAsync();
        
        while (_allConnections.TryTake(out var connection))
        {
            connection.Destroy();
        }

        _db.Dispose();
    }

    public DuckDB DuckDBQueryEngine => this;

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
        var result = ((IQueryMixin)this).Query<(string, string)>("EXPLAIN (FORMAT JSON) " + query);

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

    public ushort RegisterGlobalObject(object obj)
    {
        var globalId = (ushort)Interlocked.Increment(ref _nextGlobalId);
        _globalIds[obj] = globalId;
        _globalObjects[globalId] = obj;
        return globalId;
    }

    public void DisposeGlobalObject(object obj)
    {
        var id = _globalIds[obj];
        _globalIds.TryRemove(obj, out _);
        _globalObjects.TryRemove(id, out _);
    }

    public ushort IdFor(object conn)
    {
        return _globalIds[conn];
    }

    public object ObjectFor(ushort id)
    {
        return _globalObjects[id];
    }
}
