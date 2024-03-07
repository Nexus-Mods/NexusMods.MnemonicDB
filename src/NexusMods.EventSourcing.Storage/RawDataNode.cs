using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

/// <summary>
/// The event sourcing storage system works in data chunks to reduce overhead of iteration and other operations.
/// each node has a fixed size, and the data is stored in a contiguous memory block with each part of the node
/// being a linear array of the same type, so something like (EntityId[], TxId[], ushort[], DatomFlags[]) etc.
/// This allows data to be packed, and iteration to happen in a linear and even vectorized fashion.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = DataNodeSize)]
public unsafe struct RawDataNode
{
    /// <summary>
    /// Size of the vector, must be a power of 16 for maximum vectorization support.
    /// </summary>
    public const int DefaultNodeSize = 2048;

    /// <summary>
    /// The size of the DataChunk node in bytes.
    /// </summary>
    public const int DataNodeSize =
        (DefaultNodeSize * (sizeof(ulong) + sizeof(ulong) + sizeof(ushort) + sizeof(byte) + sizeof(ulong))) +
        (DefaultNodeSize / 8);

    public fixed ulong EntityIdVector[DefaultNodeSize];

    public fixed ulong TxIdVector[DefaultNodeSize];

    public fixed ushort AttributeIdVector[DefaultNodeSize];

    public fixed byte FlagsVector[DefaultNodeSize];

    public fixed ulong InlinedData[DefaultNodeSize];

    /// <summary>
    /// A reference to a block of memory that contains the outlined data for this node, this is data
    /// that is too large to be inlined into the node column itself (larger than 8 bytes), like strings.
    /// </summary>
    public readonly Memory<byte> OutlinedData;
}

