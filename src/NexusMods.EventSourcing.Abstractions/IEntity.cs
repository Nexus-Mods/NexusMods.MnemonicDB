using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// The base interface for all entities.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// The globally unique identifier of the entity.
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// The context this entity belongs to.
    /// </summary>
    public IEntityContext Context { get; }


    /// <summary>
    /// The type descriptor for all entities. Emitted by the <see cref="IEventContext.New{TType}"/> method.
    /// </summary>
    public static readonly AttributeDefinition<IEntity, Type> TypeAttribute = new("$Type");
}
