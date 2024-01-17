using System;
using System.ComponentModel;
using NexusMods.EventSourcing.Abstractions.AttributeDefinitions;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// The base interface for all entities.
/// </summary>
public interface IEntity : INotifyPropertyChanged
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
    public static readonly TypeAttributeDefinition TypeAttribute = new();

    /// <summary>
    /// Meta attribute for the entity id.
    /// </summary>
    public static readonly EntityIdDefinition EntityIdAttribute = new();

    /// <summary>
    /// Called when a property of the entity has changed.
    /// </summary>
    /// <param name="name"></param>
    public void OnPropertyChanged(string name);
}
