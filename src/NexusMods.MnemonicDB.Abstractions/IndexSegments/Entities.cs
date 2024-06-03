using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A subview of an IndexSegment that returns a specific entity type
/// </summary>
public readonly struct Entities<TInner, TEntity>(TInner ids, IDb db) :
    IReadOnlyCollection<TEntity>
    where TEntity : IHasEntityIdAndDb
    where TInner : IIndexSegment<EntityId>
{
    /// <summary>
    /// Gets the entity at the given index
    /// </summary>
    public TEntity this[int idx] => db.Get<TEntity>(ids[idx]);

    /// <summary>
    /// The number of entities in the collection
    /// </summary>
    public int Count => ids.Count;

    /// <inheritdoc />
    public IEnumerator<TEntity> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
