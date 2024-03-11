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
        var stats = GetStatistics();

        // Empty column
        if (stats.Count == 0)
            return new Constant<T>(default, 0);

        // Constant column
        if (stats is { ValueBits: 0, PartitionBits: 0 })
            return new Constant<T>(stats.MinValue, stats.Count);

        // MinMax column

        var data = MemoryPool<byte>.Shared.Rent(stats.MemorySize);
        var srcSpan = MemoryMarshal.Cast<T, ulong>(Span);

        var header = MemoryMarshal.Cast<byte, LowLevelHeader>(data.Memory.Span);
        header[0].Type = LowLevelType.Packed;
        header[0].Length = stats.Count;
        header[0].Packed.ValueOffset = stats.MinValue;
        header[0].Packed.PartitionOffset = stats.MinPartition;
        header[0].Packed.ValueBytes = stats.TotalBytes;
        header[0].Packed.PartitionBits = stats.PartitionBits;

        var destSpan = header[0].DataSpan(data.Memory.Span);

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
            BinaryPrimitives.WriteUInt64LittleEndian(slice, packedValue);
        }


        return new OnHeapPacked<T>(data);
    }

    public Statistics GetStatistics()
    {
        const ulong valueMask = 0x00FFFFFFFFFFFFFF;
        const byte partitionShift = 8 * 7;

        Statistics stats = new()
        {
            MinValue = ulong.MaxValue,
            MinPartition = byte.MaxValue,
        };

        var cast = MemoryMarshal.Cast<T, ulong>(Span);
        for (var i = 0; i < Span.Length; i++)
        {
            var value = cast[i];
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
        }

        return stats;
    }




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
        /// Number of bytes used to store the column in memory including the header and extra space for alignment.
        /// </summary>
        public unsafe int MemorySize => sizeof(LowLevelPacked) + (Count * TotalBytes) + 8;
    }
}
