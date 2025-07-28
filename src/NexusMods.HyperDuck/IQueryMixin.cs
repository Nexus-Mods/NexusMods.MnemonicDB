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
    public Database DuckDBQueryEngine { get; }

    public TResult Query<TResult>(CompiledQuery<TResult> query) where TResult : new() 
        => DuckDBQueryEngine.Query(query);
    
    public void QueryInto<TResult>(CompiledQuery<TResult> query, ref TResult returnValue)
        => DuckDBQueryEngine.QueryInto(query, ref returnValue);
    
    public TResult Query<TResult, TArg1>(CompiledQuery<TResult, TArg1> query, TArg1 arg1) where TResult : new() 
        => DuckDBQueryEngine.Query(query, arg1);
    
    public void QueryInto<TResult, TArg1>(CompiledQuery<TResult, TArg1> query, TArg1 arg1, ref TResult returnValue)
        => DuckDBQueryEngine.QueryInto(query, arg1, ref returnValue);
    
    public IDisposable ObserveInto<TResult>(CompiledQuery<TResult> query, ref TResult target)
        => DuckDBQueryEngine.ObserveQuery(query, ref target);
    
    public Task FlushQueries() => DuckDBQueryEngine.FlushQueries();

}
