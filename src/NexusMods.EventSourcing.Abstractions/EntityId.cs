using System;
using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

[ValueObject<Guid>]
public readonly partial struct EntityId
{
    public EntityId<T> Cast<T>() where T : IEntity => new(this);

}


/// <summary>
/// A strongly typed <see cref="EntityId"/> for a specific <see cref="IEntity"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct EntityId<T> where T : IEntity
{
    /// <summary>
    /// Creates a new instance of <see cref="EntityId{T}"/>.
    /// </summary>
    /// <returns></returns>
    public static EntityId<T> NewId() => new(EntityId.NewId());


    /// <summary>
    /// Gets the <see cref="EntityId{T}"/> from the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityId<T> From(Guid id) => new(EntityId.From(id));



    /// <summary>
    /// Gets the <see cref="EntityId{T}"/> from the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityId<T> From(string id) => From(Guid.Parse(id));


    /// <summary>
    /// Creates a new instance of <see cref="EntityId{T}"/>.
    /// </summary>
    /// <param name="id"></param>
    public EntityId(EntityId id) => Value = id;

    /// <summary>
    /// The underlying value.
    /// </summary>
    public readonly EntityId Value;

    /// <inheritdoc />
    public override string ToString()
    {
        return typeof(T).Name + "<" + Value.Value + ">";
    }
}
