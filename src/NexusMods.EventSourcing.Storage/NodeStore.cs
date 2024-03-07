using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.DatomStorageStructures;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage;

public class NodeStore(ILogger<NodeStore> logger, IKvStore kvStore, AttributeRegistry registry)
: INodeStore
{
    private ulong _txLogId = Ids.MinId(Ids.Partition.TxLog);
    private ulong _nextBlockId = Ids.MinId(Ids.Partition.Index);

    /// <summary>
    /// Writes the node to the store as the txLog block, returns the next txId
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public StoreKey LogTx(IDataNode packed)
    {
        var thisTx = ++_txLogId;
        Interlocked.Exchange(ref _nextBlockId, Ids.MakeId(Ids.Partition.Index, thisTx << 16));
        _nextBlockId = Ids.MakeId(Ids.Partition.Index, thisTx << 16);
        var logId = Ids.MakeId(Ids.Partition.TxLog, thisTx);

        var key = StoreKey.From(logId);
        Flush(key, (PackedNode)packed);
        return key;
    }

    private StoreKey NextBlockId()
    {
        return StoreKey.From(Interlocked.Increment(ref _nextBlockId));
    }

    public IDataNode Flush(IDataNode node)
    {
        return node switch
        {
            PackedNode packedChunk => Flush(packedChunk),
            PackedIndexNode packedIndexChunk => Flush(packedIndexChunk),
            _ => throw new NotImplementedException("Unknown node type. " + node.GetType().Name)
        };
    }

    public void PutRoot(DatomStoreState state)
    {
        var writer = new PooledMemoryBufferWriter();
        state.WriteTo(writer);
        var writtenSpan = writer.GetWrittenSpan();
        kvStore.Put(StoreKey.RootKey, writtenSpan);
    }

    public IDataNode Flush(IIndexNode node)
    {
        return node switch
        {
            PackedIndexNode packedIndexChunk => Flush(packedIndexChunk),
            _ => throw new NotImplementedException("Unknown node type. " + node.GetType().Name)
        };
    }

    public IIndexNode Flush(PackedIndexNode node)
    {
        var writer = new PooledMemoryBufferWriter();
        node.WriteTo(writer);
        var writtenSpan = writer.GetWrittenSpan();

        var id = NextBlockId();
        kvStore.Put(id, writtenSpan);
        return new ReferenceIndexNode(this, id, null);
    }

    public IDataNode Flush(PackedNode node)
    {
        var writer = new PooledMemoryBufferWriter();
        node.WriteTo(writer);
        var writtenSpan = writer.GetWrittenSpan();

        var id = NextBlockId();
        kvStore.Put(id, writtenSpan);
        return new ReferenceNode(this, id, null);
    }

    private void Flush(StoreKey key, PackedNode node)
    {
        var writer = new PooledMemoryBufferWriter();
        node.WriteTo(writer);
        var writtenSpan = writer.GetWrittenSpan();
        kvStore.Put(key, writtenSpan);
    }



    public IDataNode Load(StoreKey id)
    {
        if (!kvStore.TryGet(id, out var value))
        {
            throw new InvalidOperationException("Node not found");
        }

        var memory = GC.AllocateUninitializedArray<byte>(value.Length);
        value.CopyTo(memory);

        var reader = new BufferReader(memory);
        var fourcc = reader.ReadFourCC();

        if (fourcc == FourCC.PackedIndex)
        {
            return PackedIndexNode.ReadFrom(ref reader, this, registry);

        }

        if (fourcc == FourCC.PackedData)
        {
            return PackedNode.ReadFrom(ref reader);
        }


        throw new NotImplementedException("Unknown node type. " + fourcc);
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
    }


}
