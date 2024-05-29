using System;
using Vogen;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Represents a partition id
/// </summary>
[ValueObject<byte>]
public partial struct PartitionId
{
    /// <summary>
    /// Where attributes are stored
    /// </summary>
    public static PartitionId Attribute = From(0);

    /// <summary>
    /// Transaction partition
    /// </summary>
    public static PartitionId Transactions = From(1);

    /// <summary>
    /// Default partition for entities
    /// </summary>
    public static PartitionId Entity = From(2);

    /// <summary>
    /// Default partition for temporary entities
    /// </summary>
    public static PartitionId Temp = From(3);

    /// <summary>
    /// Creates a partition id for the user partition
    /// </summary>
    public static PartitionId User(byte id)
    {
        if (id <= Temp.Value)
            throw new ArgumentException($"User partitions must be greater than {Temp.Value}", nameof(id));
        return From(id);
    }

    /// <inheritdoc />
    public override string ToString() => $"PartId:{Value:x}";

    /// <summary>
    /// Encode a partition id and entity id pair
    /// </summary>
    public EntityId MakeEntityId(ulong id) => EntityId.From(((ulong)Value << 56) | (id & 0x00FFFFFFFFFFFFFF));

    /// <summary>
    /// Returns true if the given id is in this partition
    /// </summary>
    public bool InThisPartition(ulong id) => id >> 56 == Value;
}
