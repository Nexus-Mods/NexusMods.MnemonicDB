using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

public class MultiEntityAttributeDefinition<TOwner, TType>(string name) :
    ACollectionAttribute<TOwner, EntityId<TType>>(name) where TOwner : IEntity
    where TType : IEntity
{
    public IEnumerable<TType> GetAll(TOwner owner)
    {
        var tmp = owner.Context.GetAccumulator<TOwner, MultiEntityAttributeDefinition<TOwner, TType>>(owner.Id, this);
        var ids = (HashSet<EntityId<TType>>) tmp.Get();
        foreach (var id in ids)
        {
            yield return owner.Context.Get(id);
        }
    }
}
