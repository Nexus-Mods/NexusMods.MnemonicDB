using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
/// Simplistic context that just logs the entities that were modified.
/// </summary>
public readonly struct ModifiedEntitiesIngester() : IEventContext
{
    /// <summary>
    /// The entities that were modified.
    /// </summary>
    public readonly HashSet<EntityId> Entities  = new();

    /// <inheritdoc />
    public bool GetAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> entityId, TAttribute attributeDefinition,
        out TAccumulator accumulator)
        where TOwner : IEntity
        where TAttribute : IAttribute<TAccumulator>
        where TAccumulator : IAccumulator
    {
        Entities.Add(entityId.Id);
        accumulator = default!;
        return false;
    }
}
