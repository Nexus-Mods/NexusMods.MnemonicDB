using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

/// <summary>
/// A conjunction of goals, think of this as a logical AND
/// </summary>
public record Conjunction(IGoal[] Goals) : IGoal
{
    public IArgument[] Arguments => throw new NotImplementedException();
    public bool IsSupported(Span<BindingType> bindingTypes)
    {
        throw new NotImplementedException();
    }

    public Environment.Execute Emit(EnvironmentDefinition env, Environment.Execute innerExpr)
    {
        foreach (var goal in Goals.Reverse())
        {
            innerExpr = goal.Emit(env, innerExpr);
        }
        return innerExpr;
    }
}
