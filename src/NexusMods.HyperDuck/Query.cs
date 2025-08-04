using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.HyperDuck.Internals;

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
        for (int i = 0; i < Parameters.Length; i++)
            // DuckDB Parameters are 1 indexed
            prepared.Bind(i + 1, Parameters[i]);
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
        var compiled = new CompiledQuery<T>(Sql);
        using var conn = DuckDBQueryEngine.Connect();
        var prepared = conn.Prepare(compiled);
        using var result = prepared.Execute();
        var adaptor = DuckDBQueryEngine.Registry.GetAdaptor<TIntoColl>(result);
        adaptor.Adapt(result, ref intoColl);
    }

    public IDisposable ObserveInto<TIntoColl>(TIntoColl intoColl)
    {
        var deps = DuckDBQueryEngine.GetReferencedFunctions(Sql);
        QueryInto<TIntoColl>(ref intoColl);

        var live = new Internals.LiveQuery<TIntoColl>
        {
            DependsOn = deps.ToArray(),
            DuckDb = this.DuckDBQueryEngine,
            Query = new Query<TIntoColl>
            {
                Sql = Sql,
                Parameters = Parameters,
                DuckDBQueryEngine = DuckDBQueryEngine,
            },
            Output = intoColl,
            Updater = DuckDBQueryEngine.LiveQueryUpdater.Value
        };

        DuckDBQueryEngine.LiveQueryUpdater.Value.Add(live);
        return live;
    }
}
