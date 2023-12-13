namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// The base class for all entities.
/// </summary>
public abstract class AEntity(IEntityContext context, EntityId id) : IEntity
{
    public EntityId Id => id;

    public IEntityContext Context => context;

}
