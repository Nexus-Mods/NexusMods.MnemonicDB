using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public partial class DataNode : IDatomResult
{
    public long Length => NumberOfDatoms;


    public void Fill(long offset, ref DatomChunk chunk)
    {
        var count = (int)Math.Min(DatomChunk.ChunkSize, NumberOfDatoms - offset);
        EntityIds.CopyTo((int)offset, chunk.EntityIds.CastFast<EntityId, ulong>().SliceFast(0, count));
        AttributeIds.CopyTo((int)offset, chunk.AttributeIds.CastFast<AttributeId, ulong>().SliceFast(0, count));
        TransactionIds.CopyTo((int)offset, chunk.TransactionIds.CastFast<TxId, ulong>().SliceFast(0, count));

        // TODO, improve the performance of this
        for (var i = 0; i < count; i++)
        {
            chunk.SetValue(i, Values[(int)offset + i]);
        }

        chunk.SetMaskToCount(count);
    }

    public EntityId GetEntityId(long idx)
    {
        return EntityId.From(EntityIds[(int)idx]);
    }

    public AttributeId GetAttributeId(long idx)
    {
        return AttributeId.From(AttributeIds[(int)idx]);
    }

    public TxId GetTransactionId(long idx)
    {
        return TxId.From(TransactionIds[(int)idx]);
    }

    public ReadOnlySpan<byte> GetValue(long idx)
    {
        return Values[(int)idx];
    }

    public ReadOnlyMemory<byte> GetValueMemory(long idx)
    {
        return Values.GetMemory((int)idx);
    }
}
