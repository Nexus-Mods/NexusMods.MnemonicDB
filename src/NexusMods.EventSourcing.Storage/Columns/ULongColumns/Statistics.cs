﻿using System;
using System.Numerics;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public struct Statistics
{
    /// <summary>
    /// The minimum value in the column.
    /// </summary>
    public ulong MinValue;

    /// <summary>
    /// The maximum value in the column.
    /// </summary>
    public ulong MaxValue;

    /// <summary>
    /// The minimum partition value in the column.
    /// </summary>
    public byte MinPartition;

    /// <summary>
    /// The maximum partition value in the column.
    /// </summary>
    public byte MaxPartition;

    /// <summary>
    /// The total count of values in the column.
    /// </summary>
    public int Count;

    /// <summary>
    /// Number of bits used for the partition.
    /// </summary>
    public byte PartitionBits { get; set; }

    /// <summary>
    /// Number of bits used for the value.
    /// </summary>
    public byte ValueBits { get; set; }

    /// <summary>
    /// Number of bytes used to store the (partition + value) in the column.
    /// </summary>
    public byte TotalBytes { get; set; }

    /// <summary>
    /// Creates a new <see cref="Statistics"/> instance from the given <paramref name="src"/> by analyzing
    /// the values to try and determine the best way to pack the values
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public static Statistics Create(ReadOnlySpan<ulong> src)
    {
        const ulong valueMask = 0x00FFFFFFFFFFFFFF;
        const byte partitionShift = 8 * 7;

        Statistics stats = new()
        {
            MinValue = ulong.MaxValue,
            MinPartition = byte.MaxValue,
        };

        for (var i = 0; i < src.Length; i++)
        {
            var value = src[i];
            stats.MinValue = Math.Min(stats.MinValue, value & valueMask);
            stats.MaxValue = Math.Max(stats.MaxValue, value & valueMask);

            stats.MinPartition = Math.Min(stats.MinPartition, (byte)(value >> partitionShift));
            stats.MaxPartition = Math.Max(stats.MaxPartition, (byte)(value >> partitionShift));
            stats.Count++;
        }

        var partitionDelta = (ulong)stats.MaxPartition - stats.MinPartition;
        var valueDelta = stats.MaxValue - stats.MinValue;

        int partitionBits;
        if (partitionDelta == 0)
            partitionBits = 0;
        else
            partitionBits = BitOperations.Log2(partitionDelta) + 1;

        int valueBits;
        if (valueDelta == 0)
            valueBits = 0;
        else
            valueBits = BitOperations.Log2(valueDelta) + 1;

        var totalBits = partitionBits + valueBits;
        var totalBytes = totalBits / 8 + (totalBits % 8 > 0 ? 1 : 0);

        stats.PartitionBits = (byte)partitionBits;
        stats.ValueBits = (byte)valueBits;
        stats.TotalBytes = (byte)totalBytes;

        if (stats.Count == 0)
        {
            stats.MinValue = 0;
            stats.MinPartition = 0;
            stats.PartitionBits = 0;
            stats.TotalBytes = 0;
        }

        return stats;
    }

    public UL_Column_Union.ItemKind GetKind()
    {
        return TotalBytes switch
        {
            // No bytes are needed, just a constant value
            0 => UL_Column_Union.ItemKind.Constant,
            // Compression is worse than just storing the values, so we store the values
            8 or 9 => UL_Column_Union.ItemKind.Unpacked,
            _ => UL_Column_Union.ItemKind.Packed
        };
    }

}