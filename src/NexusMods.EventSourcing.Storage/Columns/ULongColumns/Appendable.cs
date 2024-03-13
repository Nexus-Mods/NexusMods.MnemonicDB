using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions.Columns.ULongColumns;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// An appendable column of ulong values. This stores values as a auto-expanding array of ulong values.
/// Backed by the shared memory pool
/// </summary>
/// <typeparam name="T"></typeparam>
public class Appendable : IDisposable, IAppendable, IReadable, IUnpacked
{
    public const int DefaultSize = 16;
    private IMemoryOwner<ulong> _data;
    private int _length;

    private Appendable(IMemoryOwner<ulong> data, int length)
    {
        _data = data;
        _length = length;
    }

    public static Appendable Create(int initialSize = DefaultSize)
    {
        return new Appendable(MemoryPool<ulong>.Shared.Rent(DefaultSize), 0);
    }

    public static Appendable Unpack(IReadable column)
    {
        var node = Create(column.Length);
        column.CopyTo(0, node.GetWritableSpan(column.Length));
        node.SetLength(column.Length);
        return node;
    }

    private Span<ulong> CastedSpan => _data.Memory.Span;


    public void Dispose()
    {
        _data.Dispose();
    }

    public void Append(ulong value)
    {
        Ensure(1);
        _data.Memory.Span[_length] = value;
        _length++;
    }

    private void Ensure(int i)
    {
        if (_length + i <= _data.Memory.Length) return;
        var newData = MemoryPool<ulong>.Shared.Rent(_data.Memory.Length * 2);
        _data.Memory.CopyTo(newData.Memory);
        _data.Dispose();
        _data = newData;

    }

    public void Append(ReadOnlySpan<ulong> values)
    {
        Ensure(values.Length);
        values.CopyTo(CastedSpan.Slice(_length));
        _length += values.Length;
    }

    public void Append(IEnumerable<ulong> values)
    {
        foreach (var value in values)
        {
            Append(value);
        }
    }

    public Span<ulong> GetWritableSpan(int size)
    {
        Ensure(size);
        return CastedSpan.Slice(_length, size);
    }

    public void SetLength(int length)
    {
        Ensure(_length - length);
        _length = length;
    }

    public int Length => _length;

    public void CopyTo(int offset, Span<ulong> dest)
    {
        CastedSpan.Slice(offset, dest.Length).CopyTo(dest);
    }

    public ulong this[int idx] => CastedSpan[idx];

    public IUnpacked Unpack()
    {
        var appendable = Create(Length);
        CopyTo(0, appendable.GetWritableSpan(Length));
        appendable.SetLength(Length);
        return appendable;
    }

    public ReadOnlySpan<ulong> Span => CastedSpan.SliceFast(0, _length);
/// <summary>
    /// Analyze the column and pack it into a more efficient representation, this will either be a constant
    /// value, an unpacked array, or a packed array. Packed arrays use a bit of bit twiddling to efficiently
    /// store the most common patterns of ids in the system
    /// </summary>
    public IReadable Pack()
    {
        var stats = Statistics.Create(MemoryMarshal.Cast<ulong, ulong>(Span));
        return (IReadable)Pack(stats);
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
                    Data = new Memory<byte>(Span.CastFast<ulong, byte>().SliceFast(0, sizeof(ulong) * stats.Count).ToArray()),
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

                var srcSpan = Span.CastFast<ulong, ulong>().SliceFast(0, stats.Count);
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
