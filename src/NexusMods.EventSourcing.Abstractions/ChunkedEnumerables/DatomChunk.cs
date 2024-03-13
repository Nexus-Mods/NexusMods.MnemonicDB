using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

/// <summary>
/// A chunk of datoms, stored with a fixed size and a cache friendly layout and size, with a mask to determine which
/// rows are valid. This mask pattern can be used to filter out rows from a result set and since a 64bit processor is
/// assumed, counts of active rows can be calculated with a simple popcount instruction. These chunks are also used
/// as a workaround for the rather high call overhead of the .NET enumerator pattern. This means that nodes can be iterated
/// over in chunks, and the chunk can be iterated over in a tight loop, reducing the overhead of the enumerator pattern.
///
/// The chunk itself is stored as a ref struct, and the inner data is stored in a memory pool, chunks can be quickly
/// allocated and decallocated.
///
/// The values are stored in a single Memory reference, so sources that need to use multiple memory sources will need
/// to return multiple chunks of various sizes.
/// </summary>
public struct DatomChunk : IDisposable, IEnumerable<Datom>
{
    /// <summary>
    /// Size of the chunk, in datoms, should be a multiple of 64
    /// </summary>
    public const int ChunkSize = 128;

    /// <summary>
    /// The number of 64bit words in the mask.
    /// </summary>
    public const int MaskLength = ChunkSize / 64;

    /// <summary>
    /// The size of the chunk column span in bytes.
    /// </summary>
    public const int ColumnSize = ChunkSize * sizeof(ulong) * 5 + MaskLength;

    private const int EntityIdOffset = 0;
    private const int AttributeIdOffset = EntityIdOffset + ChunkSize * sizeof(ulong);
    private const int TransactionIdOffset = AttributeIdOffset + ChunkSize * sizeof(ulong);
    private const int ValuesLengthOffset = TransactionIdOffset + ChunkSize * sizeof(ulong);
    private const int ValuesOffsetOffset = ValuesLengthOffset + ChunkSize * sizeof(uint);
    private const int MaskOffset = ValuesOffsetOffset + ChunkSize * sizeof(uint);

    private Memory<byte> _columns;
    private ReadOnlyMemory<byte> _values;
    private IMemoryOwner<byte> _owner;

    #region Constructors

    private DatomChunk(Memory<byte> columns, ReadOnlyMemory<byte> values, IMemoryOwner<byte> owner)
    {
        _columns = columns;
        _values = values;
        _owner = owner;
    }

    /// <summary>
    /// Create a new datom chunk, using the specified memory for the values.
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static DatomChunk Create(Memory<byte> values)
    {
        var owner = MemoryPool<byte>.Shared.Rent(ColumnSize);
        var columns = owner.Memory[..ColumnSize];
        return new DatomChunk(columns, values, owner);
    }

    #endregion




    #region Accessors

    /// <summary>
    /// The entity ids in the chunk.
    /// </summary>
    public Span<EntityId> EntityIds => _columns.Span.SliceFast(EntityIdOffset, sizeof(ulong) * ChunkSize).CastFast<byte, EntityId>();

    /// <summary>
    /// The attribute ids in the chunk.
    /// </summary>
    public Span<AttributeId> AttributeIds => _columns.Span.SliceFast(AttributeIdOffset, sizeof(ulong) * ChunkSize).CastFast<byte, AttributeId>();

    /// <summary>
    /// The transaction ids in the chunk.
    /// </summary>
    public Span<TxId> TransactionIds => _columns.Span.SliceFast(TransactionIdOffset, sizeof(ulong) * ChunkSize).CastFast<byte, TxId>();

    /// <summary>
    /// The lengths of the values in the chunk.
    /// </summary>
    public Span<uint> ValuesLengths => _columns.Span.SliceFast(ValuesLengthOffset, sizeof(uint) * ChunkSize).CastFast<byte, uint>();

    /// <summary>
    /// The offsets of the values in the chunk.
    /// </summary>
    public Span<uint> ValuesOffsets => _columns.Span.SliceFast(ValuesOffsetOffset, sizeof(uint) * ChunkSize).CastFast<byte, uint>();

    /// <summary>
    /// The values span in the chunk.
    /// </summary>
    public ReadOnlySpan<byte> Values => _values.Span;

    /// <summary>
    /// Get a specific value in the chunk.
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> GetValue(int idx)
    {
        var offset = ValuesOffsets[idx];
        var length = ValuesLengths[idx];
        return _values.Span.Slice((int)offset, (int)length);
    }


    /// <summary>
    /// Get a specific value in the chunk.
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public ReadOnlyMemory<byte> GetValueMemory(int idx)
    {
        var offset = ValuesOffsets[idx];
        var length = ValuesLengths[idx];
        return _values.Slice((int)offset, (int)length);
    }

    /// <summary>
    /// Mask of valid tuples in the chunk.
    /// </summary>
    public Span<ulong> Mask => _columns.Span.SliceFast(MaskOffset, MaskLength * sizeof(ulong)).CastFast<byte, ulong>();

    #endregion

    #region Enumerable Members

    /// <inheritdoc />
    public IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < ChunkSize; i++)
        {
            if (IsValid(i))
            {
                yield return new Datom
                {
                    E = EntityIds[i],
                    A = AttributeIds[i],
                    V = GetValueMemory(i),
                    T = TransactionIds[i]
                };
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Returns true if the specified index is valid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(int idx)
    {
        return IsValid(Mask, idx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ReadOnlySpan<ulong> mask, int idx)
    {
        return (mask[idx / 64] & (1UL << (idx % 64))) != 0;
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        _owner?.Dispose();
        _owner = null!;
        _columns = null!;
    }
}

internal unsafe struct Columns
{
    public fixed ulong EntityIds[DatomChunk.ChunkSize];
    public fixed ulong AttributeIds[DatomChunk.ChunkSize];
    public fixed ulong TransactionIds[DatomChunk.ChunkSize];
    public fixed ulong Masks[DatomChunk.MaskLength];
    public fixed uint ValuesLengths[DatomChunk.ChunkSize];
    public fixed uint ValuesOffsets[DatomChunk.ChunkSize];
}
