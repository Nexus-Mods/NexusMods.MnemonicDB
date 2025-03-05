using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using Reloaded.Memory.Pointers;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Represents a slice descriptor.
/// </summary>
public interface ISliceDescriptor
{
    /// <summary>
    /// Reset the iterator to either the first or last element, depending on the direction of the iterator.
    /// </summary>
    public void Reset<T>(T iterator) where T : ILowLevelIterator, allows ref struct;
    
    /// <summary>
    /// Move the iterator to the next element, which should either call `Next` or `Prev` on the iterator.
    /// </summary>
    public void MoveNext<T>(T iterator) where T : ILowLevelIterator, allows ref struct;

    /// <summary>
    /// Given the current iterator position, and this key, should we continue iterating (is the given span inside the bounds of the iterator)?
    /// </summary>
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan);
    
    /// <summary>
    /// Deconstruct the slice descriptor into its constituent parts.
    /// </summary>
    public void Deconstruct(out Datom from, out Datom to, out bool isReversed);
}
