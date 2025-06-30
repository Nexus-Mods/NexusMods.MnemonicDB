using System;
using DuckDB.NET.Native;
using JetBrains.Annotations;

namespace NexusMods.MnemonicDB.QueryV2;

public class QueryEngine : IDisposable
{
    private readonly DuckDBDatabase _duckDb;
    private DuckDBNativeConnection _dbConnection;
    
    private const string ConnectionString = ":memrory:";
    
    public QueryEngine()
    {
        if (NativeMethods.Startup.DuckDBOpen(":memory:", out _duckDb) == DuckDBState.Error)
        {
            throw new InvalidOperationException("Failed to open DuckDB in memory.");
        }
        
        if (NativeMethods.Startup.DuckDBConnect(_duckDb, out _dbConnection) == DuckDBState.Error)
        {
            throw new InvalidOperationException("Failed to connect to DuckDB.");
        }
    }

    [MustDisposeResource]
    public IQueryResult<TRow> Query<TRow>(string sql)
    {
        if (NativeMethods.Query.DuckDBQuery(_dbConnection, sql, out var res) == DuckDBState.Error)
        {
            throw new InvalidOperationException("Failed to execute query: " + sql);
        }

        return res.ToQueryResult<TRow>();
    }

    public void Dispose()
    {
        _dbConnection.Dispose();
        _duckDb.Dispose();
    }
}
