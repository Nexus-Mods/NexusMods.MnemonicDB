using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Dataflow;

public abstract class AOperator : IOperator
{
    private readonly Dictionary<Symbol,IInput> _inputs;
    private readonly Dictionary<Symbol,IOutput> _outputs;

    public AOperator(Symbol name)
    {
        Name = name;
        _inputs = new Dictionary<Symbol, IInput>();
        _outputs = new Dictionary<Symbol, IOutput>();
    }
    
    /// <summary>
    /// Adds an input to the operator
    /// </summary>
    protected void AddInput<TType, TTimestamp>(Symbol name, IInput<TType, TTimestamp> input)
        where TTimestamp : IComparable<TTimestamp>
    {
        _inputs.Add(name, input);
    }

    /// <summary>
    /// Adds an output to the operator
    /// </summary>
    protected void AddOutput<TValue, TTimestamp>(Symbol name, IOutput<TValue, TTimestamp> output)
        where TValue : notnull
        where TTimestamp : IComparable<TTimestamp>
    {
        _outputs.Add(name, output);
    }

    public Symbol Name { get; }
    public IReadOnlyDictionary<Symbol, IInput> Inputs => _inputs;
    public IReadOnlyDictionary<Symbol, IOutput> Outputs => _outputs;
}
