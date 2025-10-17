using System;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Represents a slice descriptor.
/// </summary>
public interface ISliceDescriptor
{
    /// <summary>
    /// Reset the iterator to either the first or last element, depending on the direction of the iterator.
    /// </summary>
    public void Reset<T>(T iterator, bool history = false) where T : ILowLevelIterator, allows ref struct;
    
    /// <summary>
    /// Given the current iterator position, and this key, should we continue iterating (is the given span inside the bounds of the iterator)?
    /// </summary>
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool history = false);

    /// <summary>
    /// Returns true if the slice requires total ordering from RocksDB. That is to say, the slice extends beyond the first
    /// segment in the database.
    /// For:
    ///  - EAVT - it covers more than one E value
    ///  - AEVT - it covers more than one A value
    ///  - AVET - it covers more than one A value
    ///  - VAET - it covers more than one V value
    ///  - TxLog - it covers more than one T value
    /// </summary>
    public bool IsTotalOrdered { get; }

    /// <summary>
    /// Deconstruct the slice into a start/end datom
    /// </summary>
    public void Deconstruct(out Datom fromDatom, out Datom toDatom);
}
