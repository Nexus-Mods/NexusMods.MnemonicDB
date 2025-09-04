using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.HyperDuck.Internals;

namespace NexusMods.HyperDuck;


public interface IQuery
{
    public HashedQuery Sql { get;}
    public object?[] Parameters { get; }
    public DuckDB DuckDBQueryEngine { get; }
}

/// <summary>
/// A query that returns rows of a given type
/// </summary>
public class Query<T> : IQuery, IEnumerable<T> where T : notnull
{
    // SQL string in UTF8 0 terminated format
    public required HashedQuery Sql { get; init; }

    public object?[] Parameters { get; init; } = [];
    
    public required DuckDB DuckDBQueryEngine { get; init; }

    public IEnumerator<T> GetEnumerator()
    {
        using var conn = DuckDBQueryEngine.Connect();
        var prepared = conn.PrepareAndBind(this);
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
        using var conn = DuckDBQueryEngine.Connect();
        var prepared = conn.PrepareAndBind(this);
        using var result = prepared.Execute();
        var adaptor = DuckDBQueryEngine.Registry.GetAdaptor<TIntoColl>(result);
        adaptor.Adapt(result, ref intoColl);
    }

    public IDisposable ObserveInto<TIntoColl>(TIntoColl intoColl) where TIntoColl : notnull
    {
        var deps = DuckDBQueryEngine.GetReferencedFunctions(this);
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

    public IObservable<IChangeSet<T, TKey>> Observe<TKey>(Func<T, TKey> keySelector) 
        where TKey : notnull
    {
        var observable = Observable.Create<IChangeSet<T, TKey>>(observer =>
        {
            var sourceCache = new SourceCache<T, TKey>(keySelector);
            var observerDisposable = sourceCache.Connect()
                .Subscribe(observer);
            var deps = DuckDBQueryEngine.GetReferencedFunctions(this);
            QueryInto(ref sourceCache);
            
            var live = new Internals.LiveQuery<SourceCache<T, TKey>>
            {
                DependsOn = deps.ToArray(),
                DuckDb = this.DuckDBQueryEngine,
                Query = new Query<SourceCache<T, TKey>>
                {
                    Sql = Sql,
                    Parameters = Parameters,
                    DuckDBQueryEngine = DuckDBQueryEngine,
                },
                Output = sourceCache,
                Updater = DuckDBQueryEngine.LiveQueryUpdater.Value
            };
            
            DuckDBQueryEngine.LiveQueryUpdater.Value.Add(live);
            return Disposable.Create(() =>
            {
                observerDisposable.Dispose();
                sourceCache.Dispose();
            });
        });

        return observable;
    }
}
