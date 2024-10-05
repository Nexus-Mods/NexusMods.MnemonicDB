using System;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Dataflow;

/// <summary>
/// An output of an operator
/// </summary>
public interface IOutput
{
    public Symbol Name { get; }
    
    /// <summary>
    /// The type of the values this output produces
    /// </summary>
    public Type ValueType { get; }
    
    /// <summary>
    /// The type of the timestamps this output produces
    /// </summary>
    public Type TimestampType { get; }
}

/// <summary>
/// A typed output of an operator
/// </summary>
public interface IOutput<TValue, TTimestamp> : IOutput
    where TValue : notnull 
    where TTimestamp : IComparable<TTimestamp>
{
    /// <summary>
    /// Attach the given input to this output
    /// </summary>
    /// <param name="input"></param>
    public void Attach(IInput<TValue, TTimestamp> input);
}
