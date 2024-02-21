﻿namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A lot of ids in this system are 64 bit unsigned integers. This class provides a way to partition those ids
/// into different categories. 64 bits is a lot of space, so we can use the high 8 bits to store the partition,
/// and the low 56 bits to store the id. This allows us to have 256 different partitions, each with way more ids
/// than we would ever need.
/// </summary>
public static class Ids
{
    /// <summary>
    /// Known partitions
    /// </summary>
    public enum Partition
    {
        /// <summary>
        /// Used for attribute definitions
        /// </summary>
        Attribute = 0,
        /// <summary>
        /// Stores TX entities
        /// </summary>
        Tx = 1,
        /// <summary>
        /// Main storage for entities
        /// </summary>
        Entity = 2,
        /// <summary>
        /// Temporary Ids for entities that have not been committed
        /// </summary>
        Tmp = 3,
        /// <summary>
        /// Not used yet
        /// </summary>
        Unknown = 4

    }

    /// <summary>
    /// Tags an id with a partition
    /// </summary>
    /// <param name="partition"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    private static ulong MakeId(Partition partition, ulong id)
    {
        return ((ulong)partition << 56) | (id & 0x00FFFFFFFFFFFFFF);
    }

    /// <summary>
    /// Gets the maximum id for the given partition
    /// </summary>
    /// <param name="partition"></param>
    /// <returns></returns>
    public static ulong MaxId(Partition partition)
    {
        return MakeId(partition, ulong.MaxValue);
    }

    /// <summary>
    /// Gets the minimum id for the given partition
    /// </summary>
    /// <param name="partition"></param>
    /// <returns></returns>
    public static ulong MinId(Partition partition)
    {
        return MakeId(partition, 0);
    }

    /// <summary>
    /// Gets the partition of the id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private static Partition GetPartition(ulong id)
    {
        return (id >> 56) switch
        {
            0 => Partition.Attribute,
            1 => Partition.Tx,
            2 => Partition.Entity,
            3 => Partition.Tmp,
            _ => Partition.Unknown,
        };
    }

    /// <summary>
    /// Gets the partition of the id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static Partition GetPartition(EntityId id)
    {
        return GetPartition(id.Value);
    }

    /// <summary>
    /// True if the id is in the given partition
    /// </summary>
    /// <param name="id"></param>
    /// <param name="partition"></param>
    /// <returns></returns>
    public static bool IsPartition(ulong id, Partition partition)
    {
        return GetPartition(id) == partition;
    }

}
