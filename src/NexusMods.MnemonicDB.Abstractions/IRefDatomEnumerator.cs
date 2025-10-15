using System;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// A high-performance enumerator for ref datoms, the values are returned as spans, and so these iterators
/// are allocation free aside from any internal native-level allocations.
/// </summary>
public interface IRefDatomEnumerator : IDisposable, ISpanDatomLikeRO
{
    /// <summary>
    /// Returns true if the enumerator was able to move to the next element, the slice descriptor is used to determine
    /// if the enumerator is still within range. If useHistory is true, the enumerator will scan over the history datoms
    /// instead of the current datoms. This does not merge the history datoms with the current datoms, that is the
    /// responsibility of the caller.
    /// </summary>
    public bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false)
        where TSliceDescriptor : ISliceDescriptor, allows ref struct;
    
    /// <summary>
    /// The current key-prefix and value spans combined
    /// </summary>
    public Ptr Current { get; }
    
    /// <summary>
    /// Gets the current element's value span (if any)
    /// </summary>
    public Ptr ValueSpan { get; }
    
    /// <summary>
    /// Gets the current element's extra value span (only if the value is a hashed blob)
    /// </summary>
    public Ptr ExtraValueSpan { get; }

    ReadOnlySpan<byte> ISpanDatomLikeRO.ValueSpan => ValueSpan.Span;
    ReadOnlySpan<byte> ISpanDatomLikeRO.ExtraValueSpan => ExtraValueSpan.Span;
}


public interface IRefDatomPeekingEnumerator : IRefDatomEnumerator
{
    /// <summary>
    /// If the next datom is a retraction, this will return the txId of the retraction. Only valid for history indexes
    /// and forward iteration.
    /// </summary>
    public bool TryGetRetractionId(out TxId id);
}
