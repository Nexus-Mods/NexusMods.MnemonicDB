using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.LogicEngine;

public class QueryEngine
{
    public IEnumerable<T> QueryAll<T>(IDb db, IPredicate query, LVar extract)
    {  
        query = Optimize(query, [QueryBuilder.GlobalDb], extract);
        var initialEnv = ImmutableDictionary<LVar, object>.Empty.Add(QueryBuilder.GlobalDb, db);
        foreach (var resultEnv in query.Source.Run(query, [initialEnv]))
        {
            yield return (T)resultEnv[extract];
        }
    }
    
    public IObservableList<T> ObserveAll<T>(IConnection conn, IPredicate query, LVar extract) 
        where T : notnull
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Optimizes the query, assuming that the pre-bound variables are already bound at the start
    /// of the query, and that extract is the variable to be extracted as the query result.
    /// </summary>
    private IPredicate Optimize(IPredicate query, LVar[] preBound, LVar extract)
    {
        var env = preBound.ToImmutableHashSet();
        return query.Source.Optimize(query, ref env, extract);
    }
}
