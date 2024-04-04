using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Models;

public interface IEntity
{
    /// <summary>
    /// Creates a new entity with the specified id and database.
    /// </summary>
    public static abstract IEntity Create(EntityId id, IDb db);

    /// <summary>
    /// The id of the entity.
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// The database the entity is stored in.
    /// </summary>
    public IDb Db { get; }

    /// <summary>
    /// The active transaction the entity is currently attached to (if any)
    /// </summary>
    public ITransaction? Tx { get; }
}
