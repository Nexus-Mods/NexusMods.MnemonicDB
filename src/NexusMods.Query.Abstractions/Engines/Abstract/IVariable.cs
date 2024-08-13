using System;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

public interface IVariable
{
    public string Name { get; }
    
    public Type Type { get; }
}
