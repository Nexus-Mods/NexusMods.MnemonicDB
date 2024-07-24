using System.Collections.Immutable;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

namespace NexusMods.MnemonicDB.Queryable;

public record QueryBuilder
{

    public ILVar[] InArgs { get; init; } = [];
    public ILVar[] FindArgs { get; init; } = [];
    
    public ImmutableList<IPredicate> Predicates { get; init; } = ImmutableList<IPredicate>.Empty;

    public static QueryBuilder New()
    {
        return new QueryBuilder();
    }

    public QueryBuilder In<TArg1>(out LVar<TArg1> inArg1)
    {
        inArg1 = new LVar<TArg1> { Name = "$" };
        return this with { InArgs = [inArg1] }; 
    }

    public QueryBuilder With(IPredicate predicate)
    {
        return this with { Predicates = Predicates.Add(predicate) };
    }

    public QueryBuilder Datoms()
    {
        
    }

    public QueryBuilder Find<TArg1>(LVar<TArg1> arg1)
    {
        return this with { FindArgs = [arg1] };
    }
    
}
