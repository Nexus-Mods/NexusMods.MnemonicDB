using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.Query.Abstractions.Engines.Abstract;

namespace NexusMods.Query.Abstractions.Optimizer;

public class AssignBindings
{
    public RootQuery Execute(RootQuery query)
    {
        var bindings = ImmutableDictionary<IVariable, BindingType>.Empty.ToBuilder();
        
        foreach (var input in query.Inputs)
        {
            bindings.Add(input, BindingType.Constant);
        }

        AssignBindingsToGoal(query.Goal, bindings.ToImmutable());
        return query;
    }

    private ImmutableDictionary<IVariable, BindingType> AssignBindingsToGoal(IGoal queryGoal, ImmutableDictionary<IVariable, BindingType> bindings)
    {
        switch (queryGoal)
        {
            case Conjunction conjunction:
                return AssignBindingsToGoal(conjunction, bindings);
            case Disjunction disjunction:
                throw new NotSupportedException("Disjunctions are not supported, yet");
            case Goal goal:
                return AssignBindingsToGoal(goal, bindings);
            default:
                throw new NotSupportedException("Invalid goal type");
        }
    }

    private ImmutableDictionary<IVariable, BindingType> AssignBindingsToGoal(Conjunction conjunction, ImmutableDictionary<IVariable, BindingType> bindings)
    {
        foreach (var goal in conjunction.Goals)
        {
            bindings = AssignBindingsToGoal(goal, bindings);
        }

        return bindings;
    }
    
    private ImmutableDictionary<IVariable, BindingType> AssignBindingsToGoal(Goal goal, ImmutableDictionary<IVariable, BindingType> bindings)
    {
        var bindingTypes = GC.AllocateUninitializedArray<BindingType>(goal.Arguments.Length);
        var newBindings = bindings;
        for (var i = 0; i < goal.Arguments.Length; i++)
        {
            if (goal.Arguments[i].TryGetVariable(out var variable))
            {
                if (!bindings.TryGetValue(variable, out var bindingType))
                {
                    bindingTypes[i] = BindingType.Output;
                    newBindings = newBindings.Add(variable, BindingType.Variable);
                }
                else 
                {
                    bindingTypes[i] = bindingType;
                }
            }
            else
            {
                bindingTypes[i] = BindingType.Constant;
            }
        }
        goal.BindingTypes = bindingTypes;
        return newBindings;
    }
}
