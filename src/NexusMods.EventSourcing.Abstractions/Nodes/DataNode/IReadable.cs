using System;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Abstractions.Nodes.DataNode;

public interface IReadable
{
    /// <summary>
    /// Length of the node in tuples
    /// </summary>
    public int Length { get; }

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
}
