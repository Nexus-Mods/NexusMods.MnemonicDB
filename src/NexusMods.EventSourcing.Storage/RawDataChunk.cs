using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

/// <summary>
/// The event sourcing storage system works in data chunks to reduce overhead of iteration and other operations.
/// each chunk has a fixed size, and the data is stored in a contiguous memory block with each part of the chunk
/// being a linear array of the same type, so something like (EntityId[], TxId[], ushort[], DatomFlags[]) etc.
/// This allows data to be packed, and iteration to happen in a linear and even vectorized fashion.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = DataChunkSize)]
public unsafe struct RawDataChunk
{
    /// <summary>
    /// Size of the vector, must be a power of 16 for maximum vectorization support.
    /// </summary>
    public const int DefaultChunkSize = 2048;

    /// <summary>
    /// The size of the DataChunk chunk in bytes.
    /// </summary>
    public const int DataChunkSize =
        (DefaultChunkSize * (sizeof(ulong) + sizeof(ulong) + sizeof(ushort) + sizeof(byte) + sizeof(ulong))) +
        (DefaultChunkSize / 8);

    public fixed ulong EntityIdVector[DefaultChunkSize];

    public fixed ulong TxIdVector[DefaultChunkSize];

    public fixed ushort AttributeIdVector[DefaultChunkSize];

    public fixed byte FlagsVector[DefaultChunkSize];

    public fixed ulong InlinedData[DefaultChunkSize];

    /// <summary>
    /// A reference to a block of memory that contains the outlined data for this chunk, this is data
    /// that is too large to be inlined into the chunk column itself (larger than 8 bytes), like strings.
    /// </summary>
    public readonly Memory<byte> OutlinedData;
}

