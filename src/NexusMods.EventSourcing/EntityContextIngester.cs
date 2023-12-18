using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public struct EntityContextIngester(Dictionary<IAttribute, IAccumulator> values, EntityId id) : IEventContext, IEventIngester
{
    /// <inheritdoc />
    public void Ingest(IEvent @event)
    {
        @event.Apply(this);
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
