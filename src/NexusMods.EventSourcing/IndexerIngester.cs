using System.Collections.Generic;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class IndexerIngester : IEventIngester, IEventContext
{
    public Dictionary<IIndexableAttribute, List<IAccumulator>> IndexedAttributes = new();

    public HashSet<EntityId> Ids = new();


    public bool Ingest(TransactionId id, IEvent @event)
    {
        @event.Apply(this);
        return true;
    }

    public bool GetAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> entityId, TAttribute attributeDefinition,
        out TAccumulator accumulator) where TOwner : IEntity where TAttribute : IAttribute<TAccumulator> where TAccumulator : IAccumulator
    {
        Ids.Add(entityId.Value);

        if (attributeDefinition is IIndexableAttribute indexableAttribute)
        {
            var indexedAccumulator = indexableAttribute.CreateAccumulator();

            if (IndexedAttributes.TryGetValue(indexableAttribute, out var found))
            {
                found.Add(indexedAccumulator);
            }
            else
            {
                var lst = new List<IAccumulator>();
                lst.Add(indexedAccumulator);
                IndexedAttributes[indexableAttribute] = lst;
            }

            accumulator = (TAccumulator)indexedAccumulator;
            return true;
        }

        accumulator = default!;
        return false;
    }
}
