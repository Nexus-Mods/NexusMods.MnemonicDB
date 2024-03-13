using System;
using System.Threading;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;
using NexusMods.EventSourcing.Storage.DatomStorageStructures;

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
    public StoreKey LogTx(IReadable packed)
    {
        /*
        var thisTx = ++_txLogId;
        Interlocked.Exchange(ref _nextBlockId, Ids.MakeId(Ids.Partition.Index, thisTx << 16));
        _nextBlockId = Ids.MakeId(Ids.Partition.Index, thisTx << 16);
        var logId = Ids.MakeId(Ids.Partition.TxLog, thisTx);

        var key = StoreKey.From(logId);
        Flush(key, (PackedNode)packed);
        return key;
        */
        throw new NotImplementedException();
    }

    private StoreKey NextBlockId()
    {
        return StoreKey.From(Interlocked.Increment(ref _nextBlockId));
    }

    public TxId GetNextTx()
    {
        return TxId.From(_txLogId + 1);
    }

    public bool TryGetLastTx(out TxId key)
    {
        return kvStore.TryGetLatestTx(out key);
    }

    public bool LoadRoot(out DatomStoreState state)
    {
        throw new NotImplementedException();
        /*
        if (!kvStore.TryGet(StoreKey.RootKey, out var value))
        {
            state = default!;
            return false;
        }

        var memory = GC.AllocateUninitializedArray<byte>(value.Length);
        value.CopyTo(memory);

        var reader = new BufferReader(memory);
        var fourcc = reader.ReadFourCC();

        if (fourcc != FourCC.DatomStoreStateRoot)
        {
            throw new InvalidOperationException("Root not found");
        }

        state = DatomStoreState.ReadFrom(reader, registry, this);
        return true;
        */
    }


    public IReadable Load(StoreKey key)
    {
        throw new NotImplementedException();
    }

    public StoreKey LogTx(EventSourcing.Abstractions.Columns.BlobColumns.IReadable node)
    {
        throw new NotImplementedException();
    }

    public EventSourcing.Abstractions.Columns.BlobColumns.IReadable Flush(EventSourcing.Abstractions.Columns.BlobColumns.IReadable node)
    {
        throw new NotImplementedException();
    }
}
