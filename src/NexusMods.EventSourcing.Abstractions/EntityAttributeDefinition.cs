using System;

namespace NexusMods.EventSourcing.Abstractions;

public class EntityAttributeDefinition<TOwner, TType>(string attrName) : AttributeDefinition<TOwner, EntityId<TType>>(attrName)
    where TOwner : AEntity
    where TType : IEntity
{
    public TType GetEntity(TOwner owner)
    {
        var accumulator = owner.Context.GetAccumulator<TOwner, EntityAttributeDefinition<TOwner, TType>>(owner.Id, this);
        var entityId = (EntityId<TType>) accumulator.Get();
        return owner.Context.Get(entityId);
    }
}
