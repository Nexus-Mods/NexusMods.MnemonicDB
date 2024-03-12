using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns.LowLevel;
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

        var rented = stats.Rent();

        var casted = MemoryMarshal.Cast<byte, LowLevelHeader>(rented.Memory.Span);

        switch (casted[0].Type)
        {
            // Only one value appear in the column
            case LowLevelType.Constant:
                casted[0].Constant.Value = stats.MinValue;
                return new OnHeapPacked<T>(rented);

            // Packing won't help, so just pack it down to a struct
            case LowLevelType.Unpacked:
            {
                var destSpan = MemoryMarshal.Cast<byte, ulong>(casted[0].DataSpan(rented.Memory.Span));
                var srcSpan = MemoryMarshal.Cast<T, ulong>(Span);
                srcSpan.CopyTo(destSpan);
                return new OnHeapPacked<T>(rented);
            }

            // Pack the column. This process looks at the partition byte (highest byte) and the remainder of the
            // ulong. It then diffs the highest and lowest values in each section to find the offsets. It then
            // stores the offsets and each value becomes a pair of (value, partition). The pairs always fall on
            // byte boundaries, but the bytes can be odd numbers, anywhere from 1 to 7 bytes per value. We make sure
            // the resulting chunk is large enough that we can over-read and mask values without overrunning the
            // allocated memory.
            case LowLevelType.Packed:
            {


                var srcSpan = MemoryMarshal.Cast<T, ulong>(Span);

                casted[0].Packed.ValueOffset = stats.MinValue;
                casted[0].Packed.PartitionOffset = stats.MinPartition;
                casted[0].Packed.ValueBytes = stats.TotalBytes;
                casted[0].Packed.PartitionBits = stats.PartitionBits;

                var destSpan = casted[0].DataSpan(rented.Memory.Span);

                const ulong valueMask = 0x00FFFFFFFFFFFFFFUL;

                var valueOffset = stats.MinValue;
                var partitionOffset = stats.MinPartition;

                for (var idx = 0; idx < srcSpan.Length; idx += 1)
                {
                    var srcValue = srcSpan[idx];
                    var partition = (byte)(srcValue >> (8 * 7)) - partitionOffset;
                    var value = (srcValue & valueMask) - valueOffset;

                    var packedValue = value << stats.PartitionBits | (byte)partition;
                    var slice = destSpan.SliceFast(stats.TotalBytes * idx);
                    MemoryMarshal.Write(slice, packedValue);
                }


                return new OnHeapPacked<T>(rented);
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
