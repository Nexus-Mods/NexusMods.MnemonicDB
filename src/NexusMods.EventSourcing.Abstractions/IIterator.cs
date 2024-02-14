using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A seekable iterator over a collection of datoms.
/// </summary>
public interface IIterator : IDisposable
{
    /// <summary>
    /// Gets the current entity id.
    /// </summary>
    public EntityId EntityId { get; }

    /// <summary>
    /// True if the current datom matches the given attribute.
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public bool IsAttribute<TAttribute>() where TAttribute : IAttribute;

    /// <summary>
    /// Gets the current tx id.
    /// </summary>
    public TxId TxId { get; }

    /// <summary>
    /// Get the current datom as a distinct value.
    /// </summary>
    public IDatom Current { get; }

    /// <summary>
    /// Move to the next datom, returns false if there are no more datoms.
    /// </summary>
    public bool Next();

    /// <summary>
    /// Reset the iterator to the beginning.
    /// </summary>
    public void Reset();
}
