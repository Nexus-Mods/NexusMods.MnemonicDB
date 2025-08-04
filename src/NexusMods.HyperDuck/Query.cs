using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusMods.HyperDuck;

/// <summary>
/// A query that returns rows of a given type
/// </summary>
public class Query<T> : IEnumerable<T>
{
    public required string Sql { get; init; }
    
    public object[] Parameters { get; init; } = [];
    
    public required DuckDB DuckDBQueryEngine { get; init; }

    public IEnumerator<T> GetEnumerator()
    {
        var compiled = new CompiledQuery<T>(Sql);
        using var conn = DuckDBQueryEngine.Connect();
        var prepared = conn.Prepare(compiled);
        using var result = prepared.Execute();
        var adaptor = DuckDBQueryEngine.Registry.GetAdaptor<List<T>>(result);
        List<T> returnValue = [];
        adaptor.Adapt(result, ref returnValue);
        return returnValue.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void QueryInto<TIntoColl>(ref TIntoColl intoColl)
    {
        throw new NotImplementedException();
    }

    public IDisposable ObserveInto<TIntoColl>(ref TIntoColl intoColl)
    {
        throw new NotImplementedException();
    }
}
