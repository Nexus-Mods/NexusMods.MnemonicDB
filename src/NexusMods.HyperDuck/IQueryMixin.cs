using System;
using System.Threading.Tasks;

namespace NexusMods.HyperDuck;

/// <summary>
/// An interface that can be added onto other connection-like objects to give them access to the common query
/// functions from HyperDuck. By exposing a single property (DuckDBQueryEngine) all the other methods can be
/// used to delegate to that engine
/// </summary>
public interface IQueryMixin
{
    public DuckDB DuckDBQueryEngine { get; }

    public Query<TResult> Query<TResult>(string sql, params object[] args)
        => new()
        {
            Sql = sql,
            Parameters = args,
            DuckDBQueryEngine = DuckDBQueryEngine,
        };
    
    public Task FlushQueries() => DuckDBQueryEngine.FlushQueries();

}
