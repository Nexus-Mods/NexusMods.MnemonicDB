using System.Buffers;
using System.Collections.Generic;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Abstractions;

public interface IDataChunk : IEnumerable<Datom>
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

    IDataChunk Flush(INodeStore store);
}
