using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DuckDB.NET.Native;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.QueryV2;

public class QueryEngine : IDisposable
{
    private readonly DuckDBDatabase _duckDb;
    private DuckDBNativeConnection _dbConnection;
    
    private const string ConnectionString = ":memrory:";
    
    private IConnection? _defaultConnection;
    
    public QueryEngine(IEnumerable<ModelTableDefinition> modelTables)
    {
        if (NativeMethods.Startup.DuckDBOpen(":memory:", out _duckDb) == DuckDBState.Error)
        {
            throw new InvalidOperationException("Failed to open DuckDB in memory.");
        }
        
        if (NativeMethods.Startup.DuckDBConnect(_duckDb, out _dbConnection) == DuckDBState.Error)
        {
            throw new InvalidOperationException("Failed to connect to DuckDB.");
        }

        foreach (var function in modelTables)
        {
            new ModelTableFunction(function, this).Register(_dbConnection);
        }
    }

    public void Add(IConnection connection)
    {
        _defaultConnection = connection;
    }

    public IConnection DefaultConnection()
    {
        return _defaultConnection!;
    }
    
    [MustDisposeResource]
    public IQueryResult<TRow> Query<TRow>(string sql)
    {
        if (NativeMethods.Query.DuckDBQuery(_dbConnection, sql, out var res) == DuckDBState.Error)
        {
            var errorString = Marshal.PtrToStringUTF8(NativeMethods.Query.DuckDBResultError(ref res));
            throw new InvalidOperationException("Failed to execute query: " + errorString);
        }

        return res.ToQueryResult<TRow>();
    }

    public void Dispose()
    {
        _dbConnection.Dispose();
        _duckDb.Dispose();
    }

    public void Register(TableFunction tableFunction)
    {
        tableFunction.Register(_dbConnection);
    }
}
