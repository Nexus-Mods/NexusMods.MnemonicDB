﻿using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// Represents a packed offset column. The compression format for this is fairly simple:
///
/// The offset is stored as a 64-bit unsigned integer, and is a value added to every value in
/// the column. So if the offset is 100, then the value 0 in the column is actually 100, and the
/// value of 10 in the column is actually 200. This allows for less storage space to be used for
/// columns that are often sorted and vary by a small amount across the entire column. However, this
/// approach falls apart in the case of mixed ID types in the same column (e.g. a column that contains
/// Tx IDs and Entity IDs). So an alternative approach is to store the partition of the ID in the first
/// 4 bits of every value, and the value in the remaining bits. All of this is packed into this struct
/// as 2 values: the number of bits used for the partition, and the size of a single value in the column.
///
/// Note: that ValueBytes is the number of bytes used to store the value, and is always expressed in
/// bytes as the value will never cross a byte boundary. The value may be an odd number of bytes, but
/// never an odd number of bits.
///
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct LowLevelPacked
{
    /// <summary>
    /// Number of rows in the column.
    /// </summary>
    public int Length;

    /// <summary>
    /// The number added to every value in the column.
    /// </summary>
    public ulong ValueOffset;

    /// <summary>
    /// The number added to every partition in the column.
    /// </summary>
    public byte PartitionOffset;

    /// <summary>
    /// The number of bytes used to store the partition and value.
    /// </summary>
    public byte ValueBytes;

    /// <summary>
    /// The number of bits used for the partition in the value partition pair.
    /// </summary>
    public byte PartitionBits;

    public ulong Get(ReadOnlySpan<byte> span, int idx)
    {
        var bytesMask = (1UL << (ValueBytes * 8)) - 1;

        var offset = idx * ValueBytes;
        var valAndPartition = MemoryMarshal.Read<ulong>(span.SliceFast(offset, 8)) & bytesMask;
        var partition = (valAndPartition >> (ValueBytes * 8 - PartitionBits)) + PartitionOffset << (8 * 7);
        var value = (valAndPartition & 0x00FFFFFFFFFFFFFFUL) + ValueOffset;
        return partition | value;
    }
}
