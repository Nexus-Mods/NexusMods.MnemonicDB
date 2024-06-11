using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A subview of an IndexSegment that returns a specific entity type
/// </summary>
public readonly struct Entities<TInner, TEntity>(TInner ids, IDb db) :
    IReadOnlyCollection<TEntity>
    where TEntity : IReadOnlyModel<TEntity>
    where TInner : IIndexSegment<EntityId>
{
    /// <summary>
    /// Gets the entity at the given index
    /// </summary>
    public TEntity this[int idx] => TEntity.Create(db, ids[idx]);

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


/// <summary>
/// A wrapper around EntityIds that auto-creates the given ReadModel on-the-fly
/// </summary>
/// <typeparam name="TModel"></typeparam>
public readonly struct Entities<TModel> : IReadOnlyCollection<TModel>
where TModel : IReadOnlyModel<TModel>
{
    private readonly EntityIds _ids;
    private readonly IDb _db;

    /// <summary>
    /// A wrapper around EntityIds that auto-creates the given ReadModel on-the-fly
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public Entities(EntityIds ids, IDb db)
    {
        _ids = ids;
        _db = db;
    }


    /// <inheritdoc />
    public IEnumerator<TModel> GetEnumerator()
    {
        foreach (var id in _ids)
        {
            yield return TModel.Create(_db, id);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => _ids.Count;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Entities<{typeof(TModel).FullName ?? typeof(TModel).Name}>({Count})";
    }
}
