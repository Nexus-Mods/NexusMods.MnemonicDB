using System;
using NexusMods.EventSourcing.Abstractions.Models;

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
    /// Seeks to the data for the given Entity Id, this implicitly resets the iterator.
    /// </summary>
    /// <param name="entityId"></param>
    public void SeekTo(EntityId entityId);

    /// <summary>
    /// Gets the current datom as a distinct value.
    /// </summary>
    public IDatom Current { get; }

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
