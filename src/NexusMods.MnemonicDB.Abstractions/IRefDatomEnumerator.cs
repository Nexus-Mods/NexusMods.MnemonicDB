using System;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IRefDatomEnumerator : IDisposable
{
    /// <summary>
    /// Returns true if the enumerator was able to move to the next element
    /// </summary>
    public bool MoveNext();
    
    /// <summary>
    /// Gets the current element's key prefix
    /// </summary>
    public KeyPrefix KeyPrefix { get; }
    
    /// <summary>
    /// Gets the current element's value span (if any)
    /// </summary>
    public ReadOnlySpan<byte> ValueSpan { get; }
    
    /// <summary>
    /// Gets the current element's extra value span (only if the value is a hashed blob)
    /// </summary>
    public ReadOnlySpan<byte> ExtraValueSpan { get; }
}
