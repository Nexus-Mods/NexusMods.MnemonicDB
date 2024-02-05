namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Abstract class for an entity
/// </summary>
/// <param name="context"></param>
/// <param name="id"></param>
public abstract class AEntity(IDb context, EntityId id)
{
    /// <summary>
    /// The unique identifier for this entity
    /// </summary>
    public EntityId Id { get; } = id;

    /// <summary>
    /// The database context this entity is associated with
    /// </summary>
    public IDb Context { get; } = context;

}
