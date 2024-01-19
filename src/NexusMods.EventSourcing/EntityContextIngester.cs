using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
/// An ingester that can be used to ingest events into an entity context.
/// </summary>
/// <param name="values"></param>
/// <param name="id"></param>
public class EntityContextIngester(Dictionary<IAttribute, IAccumulator> values, EntityId id) : IEventContext, IEventIngester
{
    /// <summary>
    /// The number of events processed by this ingester.
    /// </summary>
    public int ProcessedEvents = 0;

    /// <summary>
    /// The last transaction id processed by this ingester.
    /// </summary>
    public TransactionId LastTransactionId = TransactionId.Min;

    /// <inheritdoc />
    public bool Ingest(TransactionId txId, IEvent @event)
    {
        LastTransactionId = txId;
        ProcessedEvents++;
        @event.Apply(this);
        return true;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> entityId, TAttribute attributeDefinition, out TAccumulator accumulator)
        where TOwner : IEntity
        where TAttribute : IAttribute<TAccumulator>
        where TAccumulator : IAccumulator

    {
        if (!entityId.Value.Equals(id))
        {
            accumulator = default!;
            return false;
        }

        if (values.TryGetValue(attributeDefinition, out var found))
        {
            accumulator = (TAccumulator)found;
            return true;
        }

        var newAccumulator = attributeDefinition.CreateAccumulator();
        values.Add(attributeDefinition, newAccumulator);
        accumulator = newAccumulator;
        return true;
    }
}
