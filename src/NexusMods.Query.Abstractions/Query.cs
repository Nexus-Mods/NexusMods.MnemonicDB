using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Query.Abstractions;

public class Query
{
    public static QueryBuilder New()
    {
        return new QueryBuilder(ImmutableList<IPredicate>.Empty);
    }
}

public struct QueryBuilder(ImmutableList<IPredicate> Predicates)
{
    
    public QueryBuilder Where<TFact, TA>(Term<TA> a) 
        where TFact : IFact<TA> where TA : notnull
    {
        return new QueryBuilder(Predicates: Predicates.Add(new Predicate<TFact, TA>(a)));
    }
    
    public QueryBuilder Where<TFact, TA, TB>(Term<TA> a, Term<TB> b) 
        where TFact : IFact<TA, TB> where TA : notnull where TB : notnull
    {
        return new QueryBuilder(Predicates: Predicates.Add(new Predicate<TFact, TA, TB>(a, b)));
    }
    
    public QueryBuilder Where<TFact, TA, TB, TC>(Term<TA> a, Term<TB> b, Term<TC> c) 
        where TFact : IFact<TA, TB, TC> where TA : notnull where TB : notnull where TC : notnull
    {
        return new QueryBuilder(Predicates: Predicates.Add(new Predicate<TFact, TA, TB, TC>(a, b, c)));
    }
    
    public QueryBuilder Declare<T>(out LVar<T> lvar) where T : notnull
    {
        lvar = new LVar<T>();
        return this;
    }
    
    public Func<IDb, IEnumerable<(TA, TB)>> ToQuery<TA, TB>(TA a, TB b) 
    {
        return _ => throw new NotImplementedException();
    }
}
