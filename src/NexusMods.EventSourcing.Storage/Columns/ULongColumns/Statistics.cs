using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

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

    public ULongPackedColumn Pack(ReadOnlySpan<ulong> span)
    {
        switch (GetKind())
        {
            // Only one value appears in the column
            case UL_Column_Union.ItemKind.Constant:
                return new ULongPackedColumn
                {
                    Length = Count,
                    Header = new UL_Column_Union(
                        new UL_Constant
                        {
                            Value = MinValue
                        }),
                    Data = Memory<byte>.Empty,
                };

            // Packing won't help, so just pack it down to a struct
            case UL_Column_Union.ItemKind.Unpacked:
            {
                return new ULongPackedColumn
                {
                    Length = Count,
                    Header = new UL_Column_Union(
                        new UL_Unpacked
                        {
                            Unused = 0
                        }),
                    Data = new Memory<byte>(span.CastFast<ulong, byte>().SliceFast(0, sizeof(ulong) * Count).ToArray()),
                };
            }

            // Pack the column. This process looks at the partition byte (highest byte) and the remainder of the
            // ulong. It then diffs the highest and lowest values in each section to find the offsets. It then
            // stores the offsets and each value becomes a pair of (value, partition). The pairs always fall on
            // byte boundaries, but the bytes can be odd numbers, anywhere from 1 to 7 bytes per value. We make sure
            // the resulting chunk is large enough that we can over-read and mask values without overrunning the
            // allocated memory.
            case UL_Column_Union.ItemKind.Packed:
            {
                var destData = GC.AllocateUninitializedArray<byte>(TotalBytes * Count + 8);

                var srcSpan = span.CastFast<ulong, ulong>().SliceFast(0, Count);
                var destSpan = destData.AsSpan();

                const ulong valueMask = 0x00FFFFFFFFFFFFFFUL;

                var valueOffset = MinValue;
                var partitionOffset = MinPartition;

                for (var idx = 0; idx < span.Length; idx += 1)
                {
                    var srcValue = srcSpan[idx];
                    var partition = (byte)(srcValue >> (8 * 7)) - partitionOffset;
                    var value = (srcValue & valueMask) - valueOffset;

                    var packedValue = value << PartitionBits | (byte)partition;
                    var slice = destSpan.SliceFast(TotalBytes * idx);
                    MemoryMarshal.Write(slice, packedValue);
                }

                return new ULongPackedColumn
                {
                    Length = Count,
                    Header = new UL_Column_Union(
                        new UL_Packed
                        {
                            ValueOffset = valueOffset,
                            PartitionOffset = partitionOffset,
                            ValueBytes = TotalBytes,
                            PartitionBits = PartitionBits
                        }),
                    Data = new Memory<byte>(destData),
                };
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}
