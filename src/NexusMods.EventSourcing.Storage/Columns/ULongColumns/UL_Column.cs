using System;
using System.Runtime.InteropServices;
using FlatSharp;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// A column backed by a FlatBuffer
/// </summary>
public partial class ULongPackedColumn : IReadable
{
    /// <summary>
    /// Create a new column from a FlatBuffer in memory.
    /// </summary>
    /// <param name="memory"></param>
    /// <returns></returns>
    public static ULongPackedColumn From(ReadOnlyMemory<byte> memory)
    {
        return Serializer.Parse(memory);
    }


    public ulong this[int idx]
    {
        get
        {
            switch (Header.Kind)
            {
                case UL_Column_Union.ItemKind.Constant:
                    return Header.Constant.Value;
                case UL_Column_Union.ItemKind.Unpacked:
                    return Data.Span.Cast<byte, ulong>()[idx];
                case UL_Column_Union.ItemKind.Packed:
                    var header = Header.Packed;
                    var span = Data.Span;
                    var bytesMask = (1UL << (header.ValueBytes * 8)) - 1;

                    var offset = idx * header.ValueBytes;
                    var valAndPartition = MemoryMarshal.Read<ulong>(span.SliceFast(offset, 8)) & bytesMask;
                    var value = (valAndPartition >> header.PartitionBits) + header.ValueOffset;
                    var partition = (valAndPartition & ((1UL << header.PartitionBits) - 1)) + header.PartitionOffset;
                    return (partition << (8 * 7)) | value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void CopyTo(int offset, Span<ulong> dest)
    {
        switch (Header.Kind)
        {
            case UL_Column_Union.ItemKind.Constant:
                dest.Fill(Header.Constant.Value);
                break;
            case UL_Column_Union.ItemKind.Unpacked:
                var srcSpan = Data.Span.Cast<byte, ulong>().SliceFast(offset, dest.Length);
                srcSpan.CopyTo(dest);
                break;
            case UL_Column_Union.ItemKind.Packed:
                var src = Data.Span;
                var header = Header.Packed;
                for (var idx = 0; idx < dest.Length; idx += 1)
                {
                    var span = src.SliceFast((idx + offset) * header.ValueBytes);
                    var valAndPartition = MemoryMarshal.Read<ulong>(span) & ((1UL << (header.ValueBytes * 8)) - 1);
                    var value = (valAndPartition >> header.PartitionBits) + header.ValueOffset;
                    var partition = (valAndPartition & ((1UL << header.PartitionBits) - 1)) + header.PartitionOffset;
                    dest[idx] = (partition << (8 * 7)) | value;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

