using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.Models;

public abstract class AEntity : IEntity
{
    protected AEntity(ITransaction tx)
    {
        // This looks like it's never null, but the framework will force-inject a null here when constructing
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (tx != null)
        {
            Tx = tx;
            Id = tx.TempId();
            Db = null!;
        }
        else
        {
            Id = EntityId.MinValue;
            Tx = null;
            Db = null!;
        }

    }

    /// <summary>
    /// The transaction the entity is currently attached to (if any)
    /// </summary>
    public ITransaction? Tx { get; }

    /// <summary>
    /// Get the reverse of a relationship.
    /// </summary>
    protected Entities<EntityIds, TModel> GetReverse<TModel>(Attribute<EntityId> attribute)
        where TModel : IEntity
    {
        return Db.GetReverse<TModel>(attribute, Id);
    }

    private IndexSegment _indexSegment = default;

    /// <summary>
    /// Get the segment of the entity, if not loaded, attempts to load it.
    /// </summary>
    public ref IndexSegment GetSegment()
    {
        if (_indexSegment.Valid)
            return ref _indexSegment;

        _indexSegment = Db.GetSegment(Id);
        return ref _indexSegment;
    }

    /// <summary>
    /// The id of the entity.
    /// </summary>
    public EntityId Id { get; internal set; }

    /// <summary>
    /// The database the entity is stored in.
    /// </summary>
    public IDb Db { get; internal set; }
}
