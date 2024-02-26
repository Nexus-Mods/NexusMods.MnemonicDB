using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IDataChunk
{
    public IColumn<EntityId> EntityIds { get; }
    public IColumn<AttributeId> AttributeIds { get; }
    public IColumn<TxId> TransactionIds { get; }
    public IColumn<DatomFlags> Flags { get; }
    public IBlobColumn Values { get; }
}
