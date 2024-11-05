using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.LogicEngine;

public class QueryEngine
{
    public IEnumerable<T> QueryAll<T>(IDb db, Predicate query, LVar<T> extract)
    {  
        query = Optimize(query, [], extract);
        var initialEnv = ImmutableDictionary<LVar, object>.Empty;
        foreach (var resultEnv in query.Run(db, query, [initialEnv]))
        {
            yield return (T)resultEnv[extract];
        }
    }
    
    /// <summary>
    /// Optimizes the query, assuming that the pre-bound variables are already bound at the start
    /// of the query, and that extract is the variable to be extracted as the query result.
    /// </summary>
    private Predicate Optimize(Predicate query, LVar[] preBound, LVar extract)
    {
        var env = preBound.ToImmutableHashSet();
        return query.Optimize(ref env, extract);
    }
}
