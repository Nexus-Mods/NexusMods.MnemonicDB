using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Models;

public abstract class AEntity : IEntity
{
    protected AEntity(EntityId id, IDb db)
    {
        Id = id;
        Db = db;
    }

    protected AEntity(ITransaction tx)
    {
        Tx = tx;
        Id = tx.TempId();
        Db = null!;
    }

    /// <summary>
    /// The transaction the entity is currently attached to (if any)
    /// </summary>
    public ITransaction? Tx { get; }

    public static IEntity Create(EntityId id, IDb db)
    {
        throw new NotSupportedException();
    }

    protected IEnumerable<TModel> GetReverse<TAttr, TModel>()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// The id of the entity.
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// The database the entity is stored in.
    /// </summary>
    public IDb Db { get; }
}
