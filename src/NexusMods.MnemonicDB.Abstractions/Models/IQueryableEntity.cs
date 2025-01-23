namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An interface for a queryable entity. Queryable entities are more like markers
/// and placeholders, for when we want to construct an abstract query that doesn't
/// load all the datoms for a specific entity. It's a way of creating a pattern
/// </summary>
public interface IQueryableEntity
{
    /// <summary>
    /// The primary attribute that defines what type of entity this is
    /// </summary>
    static abstract IAttribute PrimaryAttribute { get; }
}

/// <summary>
/// A typed version of IQueryableEntity that allows for construction of a instance of the entity
/// </summary>
public interface IQueryableEntity<out T> : IQueryableEntity where T : IQueryableEntity
{
    /// <summary>
    /// Create a new instance of the entity
    /// </summary>
    public static abstract T Create(EntityId id);
}
