using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;
using NexusMods.MnemonicDB.Queryable.KnowledgeDatabase;
using NexusMods.MnemonicDB.Queryable.TypeSystem;

namespace NexusMods.MnemonicDB.Queryable;



class TypeTupe<T>
{
    
}

public class Query
{ 
    public Definitions Definitions { get;  }
    public Query(Definitions definitions) => Definitions = definitions;

    public QueryBuilder<ArgTuple<T>> From<T>(out LVar<T> fromArg1, [CallerArgumentExpression("fromArg1")] string fromArg1Name = "$")
    {
        fromArg1 = new LVar<T> { Name = fromArg1Name };
        return new QueryBuilder<ArgTuple<T>>([fromArg1], Definitions);
    }
}

public record QueryBuilder<TArgs>
where TArgs : IArgTupe
{
    public QueryBuilder(ILVar[] inArgs, Definitions definitions)
    {
        Definitions = definitions;
        InArgs = inArgs;
    }
    
    public Definitions Definitions { get; }
    public ILVar[] InArgs { get; init; } = [];
    public ILVar[] FindArgs { get; init; } = [];
    
    public ImmutableList<IPredicate> Predicates { get; init; } = ImmutableList<IPredicate>.Empty;
    
    public QueryBuilder<TArgs> With(IPredicate predicate)
    {
        return this with { Predicates = Predicates.Add(predicate) };
    }

    public QueryBuilder<TArgs> Declare<T>(out LVar<T> lvar, [CallerArgumentExpression("lvar")] string lvarName = "?")
    {
        lvar = new LVar<T> { Name = lvarName };
        return this;
    }
    
    public BuiltQuery<TArgs, ArgTuple<TOut>> Select<TOut>(LVar<TOut> arg1)
    {
        return new BuiltQuery<TArgs, ArgTuple<TOut>>(this, [arg1]);
    }

}

public class BuiltQuery<TInput, TOutput>
where TInput : IArgTupe
{
    private readonly ImmutableList<IPredicate> _predicates;
    private readonly ILVar[] _fromArgs;
    private readonly ILVar[] _selectArgs;
    private readonly Definitions _definitions;

    public IEnumerable<IPredicate> Predicates => _predicates;
    public ILVar[] FromArgs => _fromArgs;
    public ILVar[] SelectArgs => _selectArgs;

    public BuiltQuery(QueryBuilder<TInput> queryBuilder, ILVar[] selectArgs)
    {
        _definitions = queryBuilder.Definitions;
        _fromArgs = queryBuilder.InArgs;
        _predicates = queryBuilder.Predicates;
        _selectArgs = selectArgs;
    }
    
}
