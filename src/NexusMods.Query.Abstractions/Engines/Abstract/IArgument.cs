using System;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

public interface IArgument
{
    /// <summary>
    /// The data type of the argument
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Try to get the variable associated with this argument, if it is one
    /// </summary>
    public bool TryGetVariable(out IVariable variable);
    
    public bool TryGetConstant<T>(out T constant);
}
