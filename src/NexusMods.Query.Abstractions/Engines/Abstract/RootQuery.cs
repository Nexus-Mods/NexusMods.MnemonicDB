using System;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

/// <summary>
/// Top level definition of a query. This is a combination of a single goal
/// and a set of input and a single output variable
/// </summary>
public record RootQuery(IGoal Goal, IVariable[] Inputs, IVariable Output, IVariable[] InnerVariables)
{
    public bool IsSupported(Span<BindingType> bindingTypes)
    {
        throw new NotImplementedException();
    }
}
