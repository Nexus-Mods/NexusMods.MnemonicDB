using System;
using System.Threading;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;
using NexusMods.EventSourcing.Storage.Nodes.Index;

namespace NexusMods.EventSourcing.Storage;

public class NodeStore(IKvStore kvStore, AttributeRegistry registry)
: INodeStore
{
    private ulong _txLogId = Ids.MinId(Ids.Partition.TxLog);
    private ulong _nextBlockId = Ids.MinId(Ids.Partition.Index);

    /// <summary>
    /// Writes the node to the store as the txLog block, returns the next txId
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public StoreKey LogTx(IReadable node)
    {
        using var writer = new PooledMemoryBufferWriter();
        Pack(node, writer);

        var thisTx = ++_txLogId;
        Interlocked.Exchange(ref _nextBlockId, Ids.MakeId(Ids.Partition.Index, thisTx << 16));
        _nextBlockId = Ids.MakeId(Ids.Partition.Index, thisTx << 16);
        var logId = Ids.MakeId(Ids.Partition.TxLog, thisTx);

        var key = StoreKey.From(logId);

        throw new NotImplementedException();
    }

    public StoreKey Put(IReadable node)
    {
        throw new NotImplementedException();
    }

    public IReadable Get(StoreKey key)
    {
        throw new NotImplementedException();
    }

    private void Pack(IReadable node, PooledMemoryBufferWriter writer)
    {
        switch (node)
        {
            case EventSourcing.Abstractions.Nodes.Index.IReadable index:
                //PackIndex(index, writer);
                break;

            default:
                throw new InvalidOperationException("Cant pack node");
        }
    }
}
