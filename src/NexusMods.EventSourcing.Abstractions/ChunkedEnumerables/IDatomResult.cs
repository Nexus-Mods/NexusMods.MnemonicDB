using System;

namespace NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

/// <summary>
/// A result set of datoms, this represents a stream of datoms that often should be read in chunks, but
/// also supports random access via the provided getters
/// </summary>
public interface IDatomResult
{
    /// <summary>
    /// Length of the result, in datoms.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Return a new <see cref="IChunkIterator"/> that starts at the given offset.
    /// </summary>
    public IChunkIterator From(long offset);

    /// <summary>
    /// Gets the EntityId of the datom at the given index.
    /// </summary>
    public EntityId GetEntityId(int idx);

    /// <summary>
    /// Gets the AttributeId of the datom at the given index.
    /// </summary>
    public AttributeId GetAttributeId(int idx);

    /// <summary>
    /// Gets the TransactionId of the datom at the given index.
    /// </summary>
    public TxId GetTransactionId(int idx);

    /// <summary>
    /// Gets the value of the datom at the given index.
    /// </summary>
    public ReadOnlySpan<byte> GetValue(int idx);

    /// <summary>
    /// Gets the value of the datom at the given index as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> GetValueMemory(int idx);
}
