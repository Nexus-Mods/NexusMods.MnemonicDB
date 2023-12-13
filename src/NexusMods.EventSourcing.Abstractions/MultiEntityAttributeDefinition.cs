using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

public class MultiEntityAttributeDefinition<TOwner, TType>(string name) :
    ACollectionAttribute<TOwner, EntityId<TType>>(name) where TOwner : IEntity
    where TType : IEntity
{
    public IEnumerable<TType> GetAll(TOwner owner) => throw new NotImplementedException();
}
