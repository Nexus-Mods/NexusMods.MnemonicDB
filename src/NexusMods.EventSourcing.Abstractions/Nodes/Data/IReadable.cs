using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Abstractions.Nodes.Data;

public interface IReadable : IEnumerable<Datom>
{
    /// <summary>
    /// Length of the node in tuples
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Length of the node (including children) in tuples
    /// </summary>
    public long DeepLength { get; }

    /// <summary>
    /// Gets the datom at the given index.
    /// </summary>
    /// <param name="idx"></param>
    public Datom this[int idx] { get; }

    /// <summary>
    /// Gets the last datom in the node, called out as a separate property for performance reasons as index nodes
    /// may be able to provide this without iterating the entire tree
    /// </summary>
    public Datom LastDatom { get; }

    /// <summary>
    /// Get the entity id at the given index.
    /// </summary>
    public EntityId GetEntityId(int idx);

    /// <summary>
    /// Get the attribute id at the given index.
    /// </summary>
    public AttributeId GetAttributeId(int idx);

    /// <summary>
    /// Get the transaction id at the given index.
    /// </summary>
    /// <param name="idx"></param>
    public TxId GetTransactionId(int idx);

    /// <summary>
    /// Get the value at the given index.
    /// </summary>
    public ReadOnlySpan<byte> GetValue(int idx);

    /// <summary>
    /// Fills the chunk with the data from the node, starting at the given offset in the node
    /// and copying the given length of tuples. The node should fill out any parts of the mask
    /// that are not used with 0. Returns the number of tuples actually filled into the chunk.
    /// The node is allowed to fill less than the requested number of tuples if it runs out of data,
    /// or if values are in multiple memory segments (and thus a different chunk would be needed).
    /// </summary>
    public int FillChunk(int offset, int length, ref DatomChunk chunk);


    /// <summary>
    /// The entity ids in the node, may not be efficient to call this repeatedly, prefer to use FillChunk
    /// or other introspective methods to avoid repeated allocations.
    /// </summary>
    public Columns.ULongColumns.IReadable EntityIdsColumn { get; }

    /// <summary>
    /// The attribute ids in the node, may not be efficient to call this repeatedly, prefer to use FillChunk
    /// or other introspective methods to avoid repeated allocations.
    /// </summary>
    public Columns.ULongColumns.IReadable AttributeIdsColumn { get; }

    /// <summary>
    /// The transaction ids in the node, may not be efficient to call this repeatedly, prefer to use FillChunk
    /// or other introspective methods to avoid repeated allocations.
    /// </summary>
    public Columns.ULongColumns.IReadable TransactionIdsColumn { get; }

    /// <summary>
    /// The values in the node, may not be efficient to call this repeatedly, prefer to use FillChunk
    /// or other introspective methods to avoid repeated allocations.
    /// </summary>
    public Columns.BlobColumns.IReadable ValuesColumn { get; }
}
