using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

public class PooledConnection : IDisposable
{
    private readonly Database _db;
    private readonly Connection _conn;
    private ConcurrentDictionary<CompiledQuery, PreparedStatement> _preparedStatements = new();

    public PooledConnection(Connection connection, Database db)
    {
        _db = db;
        _conn = connection;

    }

    public Connection Connection => _conn;


    internal PreparedStatement Prepare<TResult>(CompiledQuery<TResult> query)
    {
        if (_preparedStatements.TryGetValue(query, out var prepared))
            return prepared;

        var stmt = _conn.Prepare(query.Sql);
        if (!_preparedStatements.TryAdd(query, stmt))
        {
            stmt.Dispose();
            throw new InvalidOperationException("Failed to add prepared statement to cache, likely this is due to using a pooled connection from multiple threads (before being returned)");
        }

        return stmt;
    }

    internal void Destory()
    {
        _conn.Dispose();
    }
    
    public void Dispose()
    {
        _db.Return(this);
    }

    [MustDisposeResource]
    public Result Query(string sqlQuery)
    {
        var stmt = _conn.Prepare(sqlQuery);
        return stmt.Execute();
    }
}
