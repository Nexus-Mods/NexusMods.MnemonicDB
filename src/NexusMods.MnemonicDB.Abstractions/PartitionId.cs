using System;
using System.Runtime.CompilerServices;
using TransparentValueObjects;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Represents a partition id
/// </summary>
[ValueObject<byte>]
public readonly partial struct PartitionId
{
    /// <summary>
    /// Where attributes are stored
    /// </summary>
    public static readonly PartitionId Attribute = From(0);

    /// <summary>
    /// Transaction partition
    /// </summary>
    public static readonly PartitionId Transactions = From(1);

    /// <summary>
    /// Default partition for entities
    /// </summary>
    public static readonly PartitionId Entity = From(2);

    /// <summary>
    /// Default partition for temporary entities
    /// </summary>
    public static readonly PartitionId Temp = From(3);

    /// <summary>
    /// Creates a partition id for the user partition
    /// </summary>
    public static PartitionId User(byte id)
    {
        if (id <= Temp.Value)
            throw new ArgumentException($"User partitions must be greater than {Temp.Value}", nameof(id));
        return From(id);
    }

    /// <summary>
    /// Gets the minimum value for this partition
    /// </summary>
    public EntityId MinValue => EntityId.From(((ulong)Value << 56) | 0);

    /// <summary>
    /// Gets the maximum value for this partition
    /// </summary>
    public EntityId MaxValue => EntityId.From(((ulong)Value << 56) | 0x00FFFFFFFFFFFFFF);

    /// <inheritdoc />
    public override string ToString() => $"PartId:{Value:x}";

    /// <summary>
    /// Encode a partition id and entity id pair
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityId MakeEntityId(ulong id)
    {
        var e = ((ulong)Value << 56) | (id & 0x00FFFFFFFFFFFFFF);
        return Unsafe.As<ulong, EntityId>(ref e);
    }

    /// <summary>
    /// Returns true if the given id is in this partition
    /// </summary>
    public bool InThisPartition(ulong id) => id >> 56 == Value;
}
