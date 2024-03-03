namespace NexusMods.EventSourcing.Abstractions;

public interface INodeStore
{
    public StoreKey LogTx(IDataChunk node);

    public IDataChunk Flush(IDataChunk node);
}
