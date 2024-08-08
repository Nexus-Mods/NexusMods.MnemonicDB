using TransparentValueObjects;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A unique identifier for an entity.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct EntityId : IAugmentWith<JsonAugment>
{
    /// <summary>
    /// Min value for an entity id with no partition
    /// </summary>
    public static EntityId MinValueNoPartition => From(0);

    /// <summary>
    /// Max value for an entity id with no partition
    /// </summary>
    public static EntityId MaxValueNoPartition => From(ulong.MaxValue);

    /// <summary>
    /// Gets the partition id of the entity id
    /// </summary>
    public PartitionId Partition => PartitionId.From((byte)(Value >> 56));

    /// <summary>
    /// Returns true if the entity id is in the given partition
    /// </summary>
    public bool InPartition(PartitionId partitionId) => Partition == partitionId;

    /// <summary>
    /// Gets just the value portion of the id (ignoring the partition)
    /// </summary>
    public ulong ValuePortion => Value & 0x00FFFFFFFFFFFFFF;

    /// <inheritdoc />
    public override string ToString()
    {
        return "EId:" + Value.ToString("X");
    }
}
