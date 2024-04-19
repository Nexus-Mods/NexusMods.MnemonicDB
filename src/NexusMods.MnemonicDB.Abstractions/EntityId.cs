using System;
using TransparentValueObjects;

namespace NexusMods.MnemonicDB.Abstractions;

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

    public static EntityId MaxValueNoPartition => From(ulong.MaxValue);

    /// <summary>
    /// Gets the partition of the id
    /// </summary>
    public byte Partition => Ids.GetPartitionValue(this);

    /// <summary>
    /// Gets just the value portion of the id (ignoring the partition)
    /// </summary>
    public ulong ValuePortion => Value & 0x00FFFFFFFFFFFFFF;
}
