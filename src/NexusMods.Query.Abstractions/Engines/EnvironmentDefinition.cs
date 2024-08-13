using System.Collections.Generic;
using NexusMods.Query.Abstractions.Engines.Abstract;
using NexusMods.Query.Abstractions.Engines.Slots;

namespace NexusMods.Query.Abstractions.Engines;

public class EnvironmentDefinition
{
    private readonly Dictionary<IVariable,SlotDefinition> _variables;
    private int _objectOffset;
    private int _valueOffset;

    public EnvironmentDefinition()
    {
        _objectOffset = 0;
        _valueOffset = 0;
        _variables = new Dictionary<IVariable, SlotDefinition>();
    }

    public EnvironmentDefinition(RootQuery query) : this()
    {
        foreach (var variable in query.Inputs)
        {
            Add(variable);
        }
        
        foreach (var variable in query.InnerVariables)
        {
            Add(variable);
        }
    }
    
    public int ObjectSpanSize => _objectOffset;
    public int ValueSpanSize => _valueOffset;
    
    public SlotDefinition this[IVariable variable] => _variables[variable];
    
    public void Add(IVariable variable)
    {
        var definition = variable.MakeSlotDefinition(ref _objectOffset, ref _valueOffset);
        _variables.Add(variable, definition);
        
    }

    public ISlot<T> GetSlot<T>(Variable<T> variable)
    {
        var definition = _variables[variable];
        return definition.MakeSlot<T>();
    }
}
