using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Query.Abstractions.Engines.Abstract;
using NexusMods.Query.Abstractions.Optimizer;
using NexusMods.Query.Abstractions.Predicates;

namespace NexusMods.Query.Abstractions;

public record QueryBuilder
{
    public IVariable[] Inputs { get; init; } = [];
    public IVariable Output { get; init; } = null!;
    public IVariable[] InnerVariables { get; init; } = [];
    
    public ImmutableList<IGoal> Goals { get; init; } = ImmutableList<IGoal>.Empty;
    
    public static QueryBuilder New<T>(out Variable<T> variable, [CallerArgumentExpression("variable")] string caller = "")
    {
        VariableFromCallerName(out variable, caller);

        return new QueryBuilder
        {
            Inputs = [variable],
        };
    }
    
    public static QueryBuilder New()
    {
        return new QueryBuilder
        {
            Inputs = [],
        };
    }
    
    public QueryBuilder Declare<T>(out Variable<T> variable, [CallerArgumentExpression("variable")] string caller = "")
    {
        VariableFromCallerName(out variable, caller);
        return this with { InnerVariables = InnerVariables.Append(variable).ToArray() };
    }
    
    public QueryBuilder Declare<T>(out Variable<T> variable1, out Variable<T> variable2, 
        [CallerArgumentExpression("variable2")] string caller2 = "", [CallerArgumentExpression("variable1")] string caller1 = "")
    {
        VariableFromCallerName(out variable1, caller1);
        VariableFromCallerName(out variable2, caller2);
        return this with { InnerVariables = InnerVariables.Append(variable1).Append(variable2).ToArray() };
    }
    
    public QueryBuilder Declare<T>(out Variable<T> variable1, out Variable<T> variable2, out Variable<T> variable3,
        [CallerArgumentExpression("variable2")] string caller2 = "", [CallerArgumentExpression("variable1")] string caller1 = "", [CallerArgumentExpression("variable3")] string caller3 = "")
    {
        VariableFromCallerName(out variable1, caller1);
        VariableFromCallerName(out variable2, caller2);
        VariableFromCallerName(out variable3, caller3);
        return this with { InnerVariables = InnerVariables.Append(variable1).Append(variable2).Append(variable3).ToArray() };
    }

    private static void VariableFromCallerName<T>(out Variable<T> variable, string caller)
    {
        var lastPart = caller.LastIndexOf(' ');
        if (lastPart > 0)
        {
            caller = caller[(lastPart + 1)..];
        }
        
        variable = Variable<T>.New(caller);
    }


    
    public QueryBuilder Datoms<THighLevel, TLowLevel>(Argument<EntityId> e, Attribute<THighLevel, TLowLevel> attribute, Argument<THighLevel> value) 
        where THighLevel : notnull
    {
        var goal = new Goal(new Predicates.Datoms<THighLevel, TLowLevel>(), [Argument<IDb>.New(ConstantNodes.Db),  e, Argument<Attribute<THighLevel, TLowLevel>>.New(attribute), value]);
        return this with { Goals = Goals.Add(goal)};
    }
    
    public Func<IDb, TIn, List<TOut>> Return<TIn, TOut>(Variable<TOut> variable) 
        where TIn : notnull 
        where TOut : notnull
    {
        var query = new RootQuery(new Conjunction(Goals.ToArray()), Inputs.Append(ConstantNodes.Db).ToArray(), variable, InnerVariables);
        new AssignBindings().Execute(query);

        var builtExpression = new LazyBuilder();
        var built = builtExpression.Build<TIn, TOut>(query);
        
        return built;
    }
    
    public Func<IDb, List<TOut>> Return<TOut>(Variable<TOut> variable) 
        where TOut : notnull
    {
        var query = new RootQuery(new Conjunction(Goals.ToArray()), Inputs.Append(ConstantNodes.Db).ToArray(), variable, InnerVariables);
        new AssignBindings().Execute(query);

        var builtExpression = new LazyBuilder();
        var built = builtExpression.Build<TOut>(query);

        return built;
    }
}
