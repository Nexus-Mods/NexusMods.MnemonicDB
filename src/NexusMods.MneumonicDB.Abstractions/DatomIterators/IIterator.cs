using System;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

/// <summary>
///     Base interface for an iterator, that allows for moving forward and backwards
///     over a sequence of spans
/// </summary>
public interface IIterator
{
    /// <summary>
    ///     True if the iterator is valid
    /// </summary>
    public bool Valid { get; }

    /// <summary>
    ///     The current datom, this span is valid as until the next call to
    ///     .Next() or .Prev();
    /// </summary>
    public ReadOnlySpan<byte> Current { get; }

    /// <summary>
    ///     Gets the registry for the attributes used to look up attribute types based on
    ///     the attribute ids
    /// </summary>
    public IAttributeRegistry Registry { get; }

    /// <summary>
    ///     Advance the iterator to the next element
    /// </summary>
    public void Next();

    /// <summary>
    ///     Move to the previous element
    /// </summary>
    public void Prev();
}
