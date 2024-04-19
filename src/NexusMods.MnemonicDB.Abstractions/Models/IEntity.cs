using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Represents an entity in the database
/// </summary>
public interface IEntity : IEnumerable<Datom>
{
    /// <summary>
    /// The id of the entity.
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// The database the entity is stored in.
    /// </summary>
    public IDb Db { get; }

    /// <summary>
    /// Returns true if the entity contains the attribute.
    /// </summary>
    public bool Contains(IAttribute attribute);

    /// <summary>
    /// Constructs an entity from an id.
    /// </summary>
    public static abstract IEntity From(IDb db, EntityId id);
}
