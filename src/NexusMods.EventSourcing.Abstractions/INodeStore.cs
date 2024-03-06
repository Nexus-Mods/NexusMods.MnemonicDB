namespace NexusMods.EventSourcing.Abstractions;

public interface INodeStore
{
    public StoreKey LogTx(IDataNode node);

    public IDataNode Flush(IDataNode node);
}
