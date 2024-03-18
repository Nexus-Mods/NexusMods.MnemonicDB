using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Abstractions.Nodes.Data;

/// <summary>
/// A node that can be appended to, and then possibly frozen.
/// </summary>
public interface IAppendable : IReadable
{
    /// <summary>
    /// True if the node is frozen, false otherwise.
    /// </summary>
    public bool IsFrozen { get; }

    /// <summary>
    /// Freezes the node, making it read-only.
    /// </summary>
    public void Freeze();

    /// <summary>
    /// Packs the node into a packed representation.
    /// </summary>
    /// <returns></returns>
    public IPacked PackDataNode();

    /// <summary>
    /// Adds a datom to the node.
    /// </summary>
    public void Add(in Datom datom);

    /// <summary>
    /// Adds a datom to the node.
    /// </summary>
    public void Add(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId transactionId);

    /// <summary>
    /// Adds a datom to the node.
    /// </summary>
    public void Add<T>(EntityId entityId, AttributeId attributeId, IValueSerializer<T> serializer, T value, TxId transactionId);

    /// <summary>
    /// Appends the contents of the other node to this node.
    /// </summary>
    public void Add(IReadable other);

    /// <summary>
    /// Adds data from the given chunk to the node.
    /// </summary>
    public void Add(in DatomChunk chunk);


    /// <summary>
    /// Adds a collection of datoms to the node.
    /// </summary>
    public void Add(IEnumerable<Datom> datoms);
}
