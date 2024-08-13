using System;
using NexusMods.Query.Abstractions.Engines.Slots;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

public interface IVariable
{
    public string Name { get; }
    
    public Type Type { get; }
    
    /// <summary>
    /// Returns (true, _) if the variable is a reference type, (false, size) if the variable is a value type.
    /// </summary>
    public SlotDefinition MakeSlotDefinition(ref int objectOffset, ref int valueOffset);
}
