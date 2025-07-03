using System.Collections.Concurrent;
using System.Collections.Generic;
using Dapper;
using DuckDB.NET.Data;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB;

public class QueryEngine : IQueryEngine
{
    private readonly DuckDBConnection _duckDb;
    private ConcurrentDictionary<string, IConnection> _connections = new();

    public DuckDBConnection DuckDb => _duckDb;
    
    public QueryEngine(IEnumerable<IQueryFunction> functions)
    {
        _duckDb = new DuckDBConnection();
        _duckDb.ConnectionString = "Data Source=:memory:";
        _duckDb.Open();
        
        _duckDb.Execute("INSTALL ui; LOAD ui; CALL start_ui();");
        
        foreach (var function in functions)
            function.Register(_duckDb, this);
    }

    public IEnumerable<dynamic> Query(string sql)
    {
        return _duckDb.Query(sql);
    }

    public IEnumerable<T> Query<T>(string select)
    {
        return _duckDb.Query<T>(select);
    }

    public void AddConnection(IConnection c, string name)
    {
        _connections[name] = c;
    }
    
    public void Dispose()
    {
        _duckDb.Dispose();
    }

    public IConnection DefaultConnection()
    {
        return _connections["default"];
    }
}
