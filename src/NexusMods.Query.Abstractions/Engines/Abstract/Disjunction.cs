using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

/// <summary>
/// A disjunction of goals, think of this as a logical OR
/// </summary>
public record Disjunction(IGoal[] Goals) : IGoal
{
    public IArgument[] Arguments => throw new NotImplementedException();
    public bool IsSupported(Span<BindingType> bindingTypes)
    {
        throw new NotImplementedException();
    }

    public Expression Emit(Dictionary<IVariable, Expression> combinedVariables, Expression innerExpr)
    {
        throw new NotImplementedException();
    }
}
