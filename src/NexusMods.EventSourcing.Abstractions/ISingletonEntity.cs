namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marks this entity as a singleton entity, the singleton id is used to retrieve the entity from the cache.
/// </summary>
public interface ISingletonEntity
{
    public static virtual EntityId SingletonId { get; }
}
