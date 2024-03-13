using System;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Abstractions.Nodes.Data;

/// <summary>
/// A node that can be appended to, and then possibly frozen.
/// </summary>
public interface IAppendable
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
    public IPacked Pack();

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
    /// Sorts the node using the given comparator, returning a special node that can be used to read the sorted data. The original node is not modified,
    /// so this will throw an exception if the node is not frozen.
    /// </summary>
    public IReadable Sort(IDatomComparator comparator);

    /// <summary>
    /// Split the appendable into groupCount number of other nodes, each will be of approximately equal size.
    /// </summary>
    public IAppendable[] Split(int groupCount);
}
