using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public interface ICanBePacked<T> : IUnpacked<T>
    where T : struct
{
    public IReadable<T> Pack()
    {
        var stats = Statistics.Create(MemoryMarshal.Cast<T, ulong>(Span));

        var rented = stats.Rent();

        var casted = MemoryMarshal.Cast<byte, LowLevelHeader>(rented.Memory.Span);

        switch (casted[0].Type)
        {
            case LowLevelType.Constant:
                casted[0].Constant.Value = stats.MinValue;
                return new OnHeapPacked<T>(rented);
            case LowLevelType.Unpacked:
            {
                var destSpan = MemoryMarshal.Cast<byte, ulong>(casted[0].DataSpan(rented.Memory.Span));
                var srcSpan = MemoryMarshal.Cast<T, ulong>(Span);
                srcSpan.CopyTo(destSpan);
                return new OnHeapPacked<T>(rented);
            }
            case LowLevelType.Packed:
            {
                // MinMax column

                var srcSpan = MemoryMarshal.Cast<T, ulong>(Span);

                casted[0].Packed.ValueOffset = stats.MinValue;
                casted[0].Packed.PartitionOffset = stats.MinPartition;
                casted[0].Packed.ValueBytes = stats.TotalBytes;
                casted[0].Packed.PartitionBits = stats.PartitionBits;

                var destSpan = casted[0].DataSpan(rented.Memory.Span);

                const ulong partitionMask = 0xFF00000000000000UL;
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
