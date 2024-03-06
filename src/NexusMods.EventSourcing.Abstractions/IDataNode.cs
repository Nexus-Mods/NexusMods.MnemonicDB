using System.Buffers;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

public interface IDataNode : IEnumerable<Datom>
{
    public int Length { get; }
    public IColumn<EntityId> EntityIds { get; }
    public IColumn<AttributeId> AttributeIds { get; }
    public IColumn<TxId> TransactionIds { get; }
    public IColumn<DatomFlags> Flags { get; }
    public IBlobColumn Values { get; }

    public Datom this[int idx] { get; }

    /// <summary>
    /// Used for indexing, called out for optimization, B+Tree branch nodes
    /// can return their internal datoms instead of calling into the child.
    /// </summary>
    public Datom LastDatom { get; }

    void WriteTo<TWriter>(TWriter writer)
        where TWriter : IBufferWriter<byte>;

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
}
