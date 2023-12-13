using System;

namespace NexusMods.EventSourcing.Abstractions;

public class EntityAttributeDefinition<TOwner, TType>(string attrName) : AttributeDefinition<TOwner, EntityId<TType>>(attrName)
    where TOwner : AEntity
    where TType : IEntity
{
    public TType GetEntity(TOwner owner) => throw new NotImplementedException();
}
