using System;
using System.Buffers;
using System.Collections.Generic;
// ReSharper disable InconsistentNaming

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Interface for a data node. These are often the leaf nodes of a B+Tree, but the index
/// nodes also implement this interface.
/// </summary>
public interface IDataNode : IEnumerable<Datom>
{
    /// <summary>
    /// The number of datoms in the node and all of its children.
    /// </summary>
    public long DeepLength { get; }

    /// <summary>
    /// The number of datoms in the node.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Get the datom at the given index.
    /// </summary>
    /// <param name="idx"></param>
    public Datom this[int idx] { get; }

    /// <summary>
    /// Used for indexing, called out for optimization, B+Tree branch nodes
    /// can return their internal datoms instead of calling into the child.
    /// </summary>
    public Datom LastDatom { get; }

    /// <summary>
    /// Writes the node to the given writer.
    /// </summary>
    /// <param name="writer"></param>
    /// <typeparam name="TWriter"></typeparam>
    void WriteTo<TWriter>(TWriter writer)
        where TWriter : IBufferWriter<byte>;

    /// <summary>
    /// Pack this node, and write it to the given store, should return a new node that is a reference to the packed data.
    /// </summary>
    IDataNode Flush(INodeStore store);

    /// <summary>
    /// Find the index of the first datom in the node that is equal to or greater than the target datom.
    /// </summary>
    int FindEATV(int start, int end, in Datom target, IAttributeRegistry registry);


    /// <summary>
    /// Find the index of the first datom in the node that is equal to or greater than the target datom.
    /// </summary>
    int FindAVTE(int start, int end, in Datom target, IAttributeRegistry registry);

    /// <summary>
    /// Find the index of the first datom in the node that is equal to or greater than the target datom.
    /// </summary>
    int FindAETV(int start, int end, in Datom target, IAttributeRegistry registry);

    /// <summary>
    /// Given a target datom, find the index of the first datom in the node that is equal to or greater than the target datom.
    /// </summary>
    int Find(int start, int end, in Datom target, SortOrders order, IAttributeRegistry registry);

    #region Getters

    /// <summary>
    /// Gets the entity id at the given index.
    /// </summary>
    public EntityId GetEntityId(int idx);

    /// <summary>
    /// Gets the attribute id at the given index.
    /// </summary>
    public AttributeId GetAttributeId(int idx);

    /// <summary>
    /// Gets the transaction id at the given index.
    /// </summary>
    public TxId GetTransactionId(int idx);

    /// <summary>
    /// Gets the flags at the given index.
    /// </summary>
    public ReadOnlySpan<byte> GetValue(int idx);

    #endregion

}
