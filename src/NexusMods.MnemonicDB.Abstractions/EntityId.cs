using System;
using System.Security.Cryptography;
using TransparentValueObjects;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A unique identifier for an entity.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct EntityId
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
    /// Gets the partition of the id
    /// </summary>
    public byte Partition => (byte)(Value >> 56);

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
