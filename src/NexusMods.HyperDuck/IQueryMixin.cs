using System;
using System.Runtime.CompilerServices;
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

    internal Query<TResult> Query<TResult>(HashedQuery sql, object?[] args) 
        where TResult : notnull => new()
    {
        Sql = sql,
        Parameters = args,
        DuckDBQueryEngine = DuckDBQueryEngine,
    };
    
    /// <summary>
    /// Create a query that takes no parameters
    /// </summary>
    public Query<TResult> Query<TResult>(string sql) 
        where TResult : notnull => Query<TResult>(HashedQuery.Create(sql), []);

    
    public Query<TResult> Query<TResult>([InterpolatedStringHandlerArgument] InterpolatedQueryHandler handler) 
        where TResult : notnull
    {
        return handler.ToQuery<TResult>(DuckDBQueryEngine);
    }

    /// <summary>
    /// Execute statements without preparing them first
    /// </summary>
    public void ExecuteNoPrepare(string sql) => DuckDBQueryEngine.ExecuteNoPepare(sql);

    public void StartUI() => ExecuteNoPrepare("INSTALL ui; LOAD ui; CALL start_ui();");
    
    public Task FlushQueries() => DuckDBQueryEngine.FlushQueries();

}
