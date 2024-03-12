using System;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// A trait interface that provides packing functionality for an unpacked column. Could technically be
/// implemented for packed columns as well, but the cases where we would want to pack a packed column are
/// very rare, and writing only for unpacked columns is simpler and easier to optimize.
///
/// T should be a ulong sized struct, or a ulong
/// </summary>
/// <typeparam name="T"></typeparam>
public partial interface IUnpacked<T>
{
    /// <summary>
    /// Analyze the column and pack it into a more efficient representation, this will either be a constant
    /// value, an unpacked array, or a packed array. Packed arrays use a bit of bit twiddling to efficiently
    /// store the most common patterns of ids in the system
    /// </summary>
    public IReadable<T> Pack()
    {
        var stats = Statistics.Create(MemoryMarshal.Cast<T, ulong>(Span));
        return (IReadable<T>)Pack(stats);
    }
    private ULongPackedColumn Pack(Statistics stats)
    {
        switch (stats.GetKind())
        {
            // Only one value appears in the column
            case UL_Column_Union.ItemKind.Constant:
                return new ULongPackedColumn
                {
                    Length = stats.Count,
                    Header = new UL_Column_Union(
                        new UL_Constant
                        {
                            Value = stats.MinValue
                        }),
                    Data = Memory<byte>.Empty,
                };

            // Packing won't help, so just pack it down to a struct
            case UL_Column_Union.ItemKind.Unpacked:
            {
                return new ULongPackedColumn
                {
                    Length = stats.Count,
                    Header = new UL_Column_Union(
                        new UL_Unpacked
                        {
                            Unused = 0
                        }),
                    Data = new Memory<byte>(Span.CastFast<T, byte>().SliceFast(0, sizeof(ulong) * stats.Count).ToArray()),
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
                var destData = GC.AllocateUninitializedArray<byte>(stats.TotalBytes * stats.Count + 8);

                var srcSpan = Span.CastFast<T, ulong>().SliceFast(0, stats.Count);
                var destSpan = destData.AsSpan();

                const ulong valueMask = 0x00FFFFFFFFFFFFFFUL;

                var valueOffset = stats.MinValue;
                var partitionOffset = stats.MinPartition;

                for (var idx = 0; idx < Span.Length; idx += 1)
                {
                    var srcValue = srcSpan[idx];
                    var partition = (byte)(srcValue >> (8 * 7)) - partitionOffset;
                    var value = (srcValue & valueMask) - valueOffset;

                    var packedValue = value << stats.PartitionBits | (byte)partition;
                    var slice = destSpan.SliceFast(stats.TotalBytes * idx);
                    MemoryMarshal.Write(slice, packedValue);
                }

                return new ULongPackedColumn
                {
                    Length = stats.Count,
                    Header = new UL_Column_Union(
                        new UL_Packed
                        {
                            ValueOffset = valueOffset,
                            PartitionOffset = partitionOffset,
                            ValueBytes = stats.TotalBytes,
                            PartitionBits = stats.PartitionBits
                        }),
                    Data = new Memory<byte>(destData),
                };
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}
