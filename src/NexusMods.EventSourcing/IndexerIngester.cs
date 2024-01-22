using System.Collections.Generic;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
/// An ingester that indexes all the attributes of all the entities in the transaction.
/// </summary>
public class IndexerIngester : IEventIngester, IEventContext
{
    /// <summary>
    /// The attributes that were indexed.
    /// </summary>
    public readonly Dictionary<IIndexableAttribute, List<IAccumulator>> IndexedAttributes = new();

    /// <summary>
    /// The entity ids that were indexed.
    /// </summary>
    public readonly HashSet<EntityId> Ids = new();


    /// <inheritdoc />
    public bool Ingest(TransactionId id, IEvent @event)
    {
        @event.Apply(this);
        return true;
    }

    /// <inheritdoc />
    public bool GetAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> entityId, TAttribute attributeDefinition,
        out TAccumulator accumulator) where TOwner : IEntity where TAttribute : IAttribute<TAccumulator> where TAccumulator : IAccumulator
    {
        Ids.Add(entityId.Id);

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

    /// <summary>
    /// Clears the indexer ingester's state but keep the collections around for reuse.
    /// </summary>
    public void Reset()
    {
        IndexedAttributes.Clear();
        Ids.Clear();
    }
}
