using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Dataflow;

public interface IOperator
{
    /// <summary>
    /// A name for the operator
    /// </summary>
    public Symbol Name { get; }
    
    /// <summary>
    /// The inputs of the operator
    /// </summary>
    public IReadOnlyDictionary<Symbol, IInput> Inputs { get; }
    
    /// <summary>
    /// The outputs of the operator
    /// </summary>
    public IReadOnlyDictionary<Symbol, IOutput> Outputs { get; }
}
