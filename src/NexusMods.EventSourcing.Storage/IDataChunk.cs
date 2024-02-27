using System.Buffers;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IDataChunk
{
    public int Length { get; }
    public IColumn<EntityId> EntityIds { get; }
    public IColumn<AttributeId> AttributeIds { get; }
    public IColumn<TxId> TransactionIds { get; }
    public IColumn<DatomFlags> Flags { get; }
    public IBlobColumn Values { get; }

    public Datom this[int idx] { get; }

    void WriteTo<TWriter>(TWriter writer)
        where TWriter : IBufferWriter<byte>;
}
