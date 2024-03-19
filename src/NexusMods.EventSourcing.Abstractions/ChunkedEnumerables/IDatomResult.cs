using System;
using System.Collections;
using System.Collections.Generic;
using DynamicData;

namespace NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

/// <summary>
/// A result set of datoms, this represents a stream of datoms that often should be read in chunks, but
/// also supports random access via the provided getters
/// </summary>
public interface IDatomResult : IEnumerable<Datom>
{
    /// <summary>
    /// Length of the result, in datoms.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Fills the given chunk with datoms from the result, starting at the given offset in the result. Returns
    /// the number of datoms filled into the chunk.
    /// </summary>
    public void Fill(long offset, DatomChunk chunk);

    /// <summary>
    /// Write the value at offset into the chunk at idx
    /// </summary>
    public void FillValue(long offset, DatomChunk chunk, int idx);

    /// <summary>
    /// Gets the EntityId of the datom at the given index.
    /// </summary>
    public EntityId GetEntityId(long idx);

    /// <summary>
    /// Gets the AttributeId of the datom at the given index.
    /// </summary>
    public AttributeId GetAttributeId(long idx);

    /// <summary>
    /// Gets the TransactionId of the datom at the given index.
    /// </summary>
    public TxId GetTransactionId(long idx);

    /// <summary>
    /// Gets the value of the datom at the given index.
    /// </summary>
    public ReadOnlySpan<byte> GetValue(long idx);

    /// <summary>
    /// Gets the value of the datom at the given index as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> GetValueMemory(long idx);

    /// <summary>
    /// Gets the datom at the given index, this is often not the most efficient way to access the datoms, so
    /// prefer either the getters or the chunk iterator.
    /// </summary>
    public Datom this[int idx] => new() {
        E = GetEntityId(idx),
        A = GetAttributeId(idx),
        T = GetTransactionId(idx),
        V = GetValueMemory(idx)
    };

    /// <inheritdoc />
    IEnumerator<Datom> IEnumerable<Datom>.GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
        {
            yield return this[i];
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
