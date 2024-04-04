using System;
using System.Collections.Generic;

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

    protected IEnumerable<TModel> GetReverse<TAttr, TModel>()
        where TAttr : IAttribute<EntityId>
        where TModel : IEntity
    {
        return Db.GetReverse<TAttr, TModel>(Id);
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
