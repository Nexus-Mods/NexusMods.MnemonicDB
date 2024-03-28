using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
///     A unique identifier for an entity.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct EntityId
{
    /// <summary>
    ///     The minimum possible value for an entity id in the entity partition.
    /// </summary>
    public static EntityId MinValue => From(Ids.MakeId(Ids.Partition.Entity, 1));

    /// <summary>
    /// Min value for an entity id with no partition
    /// </summary>
    public static EntityId MinValueNoPartition => From(0);
}
