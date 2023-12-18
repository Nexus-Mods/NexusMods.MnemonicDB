using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Called internally when a property of the entity has changed.
    /// </summary>
    /// <param name="name"></param>
    public void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    EntityId IEntity.Id => Id.Value;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}
