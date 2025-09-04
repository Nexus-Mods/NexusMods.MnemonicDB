using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

public class PooledConnection : IDisposable
{
    private readonly DuckDB _db;
    private readonly Connection _conn;
    private ConcurrentDictionary<HashedQuery, PreparedStatement> _preparedStatements = new();
    
    public PooledConnection(Connection connection, DuckDB db)
    {
        _db = db;
        _conn = connection;
    }

    public Connection Connection => _conn;


    internal PreparedStatement Prepare(HashedQuery query)
    {
        if (_preparedStatements.TryGetValue(query, out var prepared))
            return prepared;

        var stmt = _conn.Prepare(query);
        if (!_preparedStatements.TryAdd(query, stmt))
        {
            stmt.Dispose();
            throw new InvalidOperationException("Failed to add prepared statement to cache, likely this is due to using a pooled connection from multiple threads (before being returned)");
        }

        return stmt;
    }

    internal void Destroy()
    {
        var oldStatements = _preparedStatements;
        _preparedStatements = null!;
        foreach (var prepared in oldStatements.Values)
            prepared.Dispose();
        _conn.Dispose();
    }
    
    public void Dispose()
    {
        _db.Return(this);
    }

    [MustDisposeResource]
    public Result Query(string sqlQuery)
    {
        return _conn.Query(sqlQuery);
    }

    public PreparedStatement PrepareAndBind<T>(Query<T> query) where T : notnull
    {
        var statement = Prepare(query.Sql);
        var parameters = query.Parameters.AsSpan();
        for (int i = 0; i < parameters.Length; i++)
        {
            statement.Bind((ulong)i + 1, parameters[i]);
        }
        return statement;
    }
}
