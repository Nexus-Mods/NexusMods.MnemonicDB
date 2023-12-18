namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// The base class for all entities.
/// </summary>
public abstract class AEntity<TEntity> : IEntity
    where TEntity : IEntity
{
    /// <summary>
    /// The context this entity belongs to.
    /// </summary>
    public readonly IEntityContext Context;

    /// <summary>
    /// The typed entity id.
    /// </summary>
    protected internal readonly EntityId<TEntity> Id;

    /// <summary>
    /// The base class for all entities.
    /// </summary>
    protected AEntity(IEntityContext context, EntityId<TEntity> id)
    {
        Context = context;
        Id = id;
    }

    IEntityContext IEntity.Context => Context;
    EntityId IEntity.Id => Id.Value;
}
