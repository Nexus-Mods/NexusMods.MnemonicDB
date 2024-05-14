namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An entity is a reference to the attributes of a specific EnityId. Think of this as a hashmap
/// of attributes, or a row in a database table.
/// </summary>
public class ReadOnlyModel(IDb db, EntityId id) : IHasEntityIdAndDb
{
    /// <inheritdoc />
    public EntityId Id => id;

    /// <inheritdoc />
    public IDb Db => db;
}
