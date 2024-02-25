using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents a raw datom, can only be stored in on the stack
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 32, Pack = 1)]
public struct RawDatom
{
    /// <summary>
    /// The entity id of the datom
    /// </summary>
    [FieldOffset(0)]
    public EntityId Entity;

    /// <summary>
    /// The transaction id of the datom
    /// </summary>
    [FieldOffset(8)]
    public TxId Tx;

    /// <summary>
    /// The attribute id of the datom
    /// </summary>
    [FieldOffset(16)]
    public ushort Attribute;

    /// <summary>
    /// The flags of the datom
    /// </summary>
    [FieldOffset(18)]
    public DatomFlags Flags;

    /// <summary>
    /// Fully inlined data, maximum 13 bytes
    /// </summary>
    [FieldOffset(19)]
    public unsafe fixed byte InlinedData[13];

    /// <summary>
    /// Inlined data, but aligned to 8 bytes
    /// </summary>
    [FieldOffset(24)]
    public unsafe fixed byte AlignedData[8];

    /// <summary>
    /// The aligned data value as a ulong
    /// </summary>
    [FieldOffset(24)]
    public unsafe ulong AlignedDataValue;

    /// <summary>
    /// If the data is not inlined, this is the offset to the data
    /// in some external memory location
    /// </summary>
    [FieldOffset(24)]
    public unsafe uint DataOffset;

    /// <summary>
    /// If the data is not inlined, this is the size of the data
    /// in some external memory location
    /// </summary>
    [FieldOffset(28)]
    public unsafe uint DataSize;

    /// <summary>
    /// A vector of 32 bytes covering the entire datom. Useful for using
    /// vectorized instructions to operate on the entire datom at once.
    /// </summary>
    [FieldOffset(0)]
    public Vector256<byte> Vector256;
}
