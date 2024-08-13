using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

public interface IGoal
{ 
    public IArgument[] Arguments { get; }
    bool IsSupported(Span<BindingType> bindingTypes);
    Expression Emit(Dictionary<IVariable, Expression> combinedVariables, Expression innerExpr);
}
