using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
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
    public StoreKey LogTx(IDataChunk packed)
    {
        var thisTx = ++_txLogId;
        Interlocked.Exchange(ref _nextBlockId, Ids.MakeId(Ids.Partition.Index, thisTx << 16));
        _nextBlockId = Ids.MakeId(Ids.Partition.Index, thisTx << 16);
        var logId = Ids.MakeId(Ids.Partition.TxLog, thisTx);

        var key = StoreKey.From(logId);
        Flush(key, (PackedChunk)packed);
        return key;
    }

    private StoreKey NextBlockId()
    {
        return StoreKey.From(Interlocked.Increment(ref _nextBlockId));
    }

    public IDataChunk Flush(IDataChunk node)
    {
        return node switch
        {
            PackedChunk packedChunk => Flush(packedChunk),
            PackedIndexChunk packedIndexChunk => Flush(packedIndexChunk),
            _ => throw new NotImplementedException("Unknown node type. " + node.GetType().Name)
        };
    }


    public IDataChunk Flush(IIndexChunk node)
    {
        return node switch
        {
            PackedIndexChunk packedIndexChunk => Flush(packedIndexChunk),
            _ => throw new NotImplementedException("Unknown node type. " + node.GetType().Name)
        };
    }

    public IIndexChunk Flush(PackedIndexChunk chunk)
    {
        var node = (PackedIndexChunk)chunk.Flush(this);
        var writer = new PooledMemoryBufferWriter();
        chunk.WriteTo(writer);
        var writtenSpan = writer.GetWrittenSpan();

        var id = NextBlockId();
        kvStore.Put(id, writtenSpan);
        return new ReferenceIndexChunk(this, id, null);
    }

    public IDataChunk Flush(PackedChunk node)
    {
        var writer = new PooledMemoryBufferWriter();
        node.WriteTo(writer);
        var writtenSpan = writer.GetWrittenSpan();

        var id = NextBlockId();
        kvStore.Put(id, writtenSpan);
        return new ReferenceChunk(this, id, null);
    }

    private void Flush(StoreKey key, PackedChunk chunk)
    {
        var writer = new PooledMemoryBufferWriter();
        chunk.WriteTo(writer);
        var writtenSpan = writer.GetWrittenSpan();
        kvStore.Put(key, writtenSpan);
    }



    public IDataChunk Load(StoreKey id)
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
            return PackedIndexChunk.ReadFrom(ref reader, this, registry);

        }

        if (fourcc == FourCC.PackedData)
        {
            return PackedChunk.ReadFrom(ref reader);
        }


        throw new NotImplementedException("Unknown node type. " + fourcc);
    }

    public TxId GetNextTx()
    {
        return TxId.From(_txLogId + 1);
    }
}
