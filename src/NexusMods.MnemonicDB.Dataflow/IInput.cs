using System;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Dataflow;

public interface IInput
{
    /// <summary>
    /// The name of the input
    /// </summary>
    public Symbol Name { get; }
    
    /// <summary>
    /// The type of the values this input receives
    /// </summary>
    public Type ValueType { get; }
    
    /// <summary>
    /// The type of the timestamps this input receives
    /// </summary>
    public Type TimestampType { get; }
}

public interface IInput<TType, TTimestamp> : IInput
where TTimestamp : IComparable<TTimestamp>
{
    /// <summary>
    /// Notify the operator of new values and their delta counts and the associated timestamp
    /// </summary>
    public void OnRecv(ReadOnlySpan<(TType, int)> values, TTimestamp timestamp);
    
    /// <summary>
    /// Notify the operator that the given timestamp is completed and no more values will be received for it
    /// </summary>
    public void OnNotify(TTimestamp timestamp);
}
