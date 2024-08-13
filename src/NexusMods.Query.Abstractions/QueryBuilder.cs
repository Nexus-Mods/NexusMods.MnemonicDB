using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Query.Abstractions.Engines.Abstract;
using NexusMods.Query.Abstractions.Optimizer;

namespace NexusMods.Query.Abstractions;

public record QueryBuilder
{
    public IVariable[] Inputs { get; init; } = [];
    public IVariable Output { get; init; } = null!;
    public IVariable[] InnerVariables { get; init; } = [];
    
    public ImmutableList<IGoal> Goals { get; init; } = ImmutableList<IGoal>.Empty;
    
    public static QueryBuilder New<T>(out Variable<T> variable, [CallerArgumentExpression("variable")] string caller = "")
    {
        var lastPart = caller.LastIndexOf(' ');
        if (lastPart > 0)
        {
            caller = caller[(lastPart + 1)..];
        }
        
        variable = Variable<T>.New(caller);
        
        return new QueryBuilder
        {
            Inputs = [variable],
        };
    }
    
    public QueryBuilder Declare<T>(out Variable<T> variable, [CallerArgumentExpression("variable")] string caller = "")
    {
        var lastPart = caller.LastIndexOf(' ');
        if (lastPart > 0)
        {
            caller = caller[(lastPart + 1)..];
        }
        
        variable = Variable<T>.New(caller);
        return this with { InnerVariables = InnerVariables.Append(variable).ToArray() };
    }
    
    public QueryBuilder Datoms<THighLevel, TLowLevel>(Argument<EntityId> e, Attribute<THighLevel, TLowLevel> attribute, Argument<THighLevel> value) 
        where THighLevel : notnull
    {
        var goal = new Goal(new Predicates.Datoms<THighLevel, TLowLevel>(), [Argument<IDb>.New(ConstantNodes.Db),  e, Argument<Attribute<THighLevel, TLowLevel>>.New(attribute), value]);
        return this with { Goals = Goals.Add(goal)};
    }
    
    public RootQuery Return<T>(Variable<T> variable)
    {
        var query = new RootQuery(new Conjunction(Goals.ToArray()), Inputs.Append(ConstantNodes.Db).ToArray(), variable, InnerVariables);
        new AssignBindings().Execute(query);

        var builtExpression = new ExpressionBuilder();
        
        var built = builtExpression.Build(query);
        
        return query;
    }
}
