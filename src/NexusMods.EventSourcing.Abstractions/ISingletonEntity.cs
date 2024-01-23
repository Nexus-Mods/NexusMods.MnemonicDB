namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marks this entity as a singleton entity, the singleton id is used to retrieve the entity from the cache.
/// </summary>
public interface ISingletonEntity : IEntity
{
    /// <summary>
    /// The singleton id of the entity.
    /// </summary>
    public static abstract EntityId SingletonId { get; }
}
