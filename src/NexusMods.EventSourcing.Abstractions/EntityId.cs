using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A unique identifier for an entity.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct EntityId
{

}

public readonly struct EntityId<T> where T : AEntity
{
    private EntityId(EntityId id)
    {
        Id = id;
    }

    public static EntityId<T> From(EntityId id) => new(id);

    public static EntityId<T> From(ulong id) => new(EntityId.From(id));

    /// <summary>
    /// Implicitly convert a typed entity id to an untyped entity id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator EntityId(EntityId<T> id) => id.Id;

    public readonly EntityId Id;

}
