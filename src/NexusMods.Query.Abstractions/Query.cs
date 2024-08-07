using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Query.Abstractions;

public class Query
{
    public static QueryBuilder New()
    {
        return new QueryBuilder();
    }
}

public class QueryBuilder
{
    public ImmutableList<IPredicate> Predicates { get; } = ImmutableList<IPredicate>.Empty;
    
    public QueryBuilder Where<TFact, TA>(Term<TA> a) 
        where TFact : IFact<TA> where TA : notnull
    {
        return this;
    }
    
    public QueryBuilder Where<TFact, TA, TB>(Term<TA> a, Term<TB> b) 
        where TFact : IFact<TA, TB> where TA : notnull where TB : notnull
    {
        return this;
    }
    
    public QueryBuilder Where<TFact, TA, TB, TC>(Term<TA> a, Term<TB> b, Term<TC> c) 
        where TFact : IFact<TA, TB, TC> where TA : notnull where TB : notnull where TC : notnull
    {
        return this;
    }
    
    public QueryBuilder Declare<T>(out LVar<T> lvar) where T : notnull
    {
        lvar = new LVar<T>();
        return this;
    }
    
    public Func<IDb, IEnumerable<TFact>> ToQuery<TFact, TA, TB>(TA a, TB b) 
        where TFact : IFact<TA, TB>
    {
        return _ => throw new NotImplementedException();
    }
}
