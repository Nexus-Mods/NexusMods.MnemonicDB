using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
/// Simplistic context that just logs the entities that were modified.
/// </summary>
public readonly struct ModifiedEntitiesIngester() : IEventContext
{
    public readonly HashSet<EntityId> Entities  = new();
    public void Emit<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
    {
        Entities.Add(entity.Value);
    }

    public void Emit<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
    {
        Entities.Add(entity.Value);
    }

    public void Retract<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
    {
        Entities.Add(entity.Value);
    }

    public void Retract<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
    {
        Entities.Add(entity.Value);
    }

    public void New<TType>(EntityId<TType> id) where TType : IEntity
    {
        Entities.Add(id.Value);
    }
}
