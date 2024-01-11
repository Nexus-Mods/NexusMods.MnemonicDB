using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class EntityContextIngester(Dictionary<IAttribute, IAccumulator> values, EntityId id) : IEventContext, IEventIngester
{
    public int ProcessedEvents = 0;

    /// <inheritdoc />
    public bool Ingest(TransactionId _, IEvent @event)
    {
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
