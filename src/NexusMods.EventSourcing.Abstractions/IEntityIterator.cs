using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents an iterator over a set of datoms.
/// </summary>
public interface IEntityIterator : IDisposable
{
    /// <summary>
    /// Move to the next datom for the current entity
    /// </summary>
    /// <returns></returns>
    public bool Next();

    /// <summary>
    /// Sets the EntityId for the iterator, the next call to Next() will return the first datom for the given entity
    /// that is less than or equal to the txId given to the iterator at creation.
    /// </summary>
    /// <param name="entityId"></param>
    public void Set(EntityId entityId);

    /// <summary>
    /// Gets the current datom as a distinct value.
    /// </summary>
    public Datom Current { get; }

    /// <summary>
    /// Gets the current datom's value
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public TValue GetValue<TAttribute, TValue>()
        where TAttribute : IAttribute<TValue>;

    /// <summary>
    /// Gets the current datom's attribute id
    /// </summary>
    public ulong AttributeId { get; }

    /// <summary>
    /// Gets the current datom's value as a span, valid until the next call to Next()
    /// or SetEntityId()
    /// </summary>
    public ReadOnlySpan<byte> ValueSpan { get; }
}
