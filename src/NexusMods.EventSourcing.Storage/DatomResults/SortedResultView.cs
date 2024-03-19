using System;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Storage.DatomResults;

public class SortedResultView(IDatomResult result, int[] indices) : IDatomResult
{
    public long Length => result.Length;

    public void Fill(long offset, DatomChunk chunk)
    {
        var count = (int)Math.Min(DatomChunk.ChunkSize, result.Length - offset);

        for (var idx = 0; idx < count; idx++)
        {
            chunk.EntityIds[idx] = result.GetEntityId(indices[offset + idx]);
            chunk.AttributeIds[idx] = result.GetAttributeId(indices[offset + idx]);
            chunk.TransactionIds[idx] = result.GetTransactionId(indices[offset + idx]);
            chunk.SetValue(idx, result.GetValue(indices[offset + idx]));
        }

        chunk.SetMaskToCount(count);
    }

    public void FillValue(long offset, DatomChunk chunk, int idx)
    {
        result.FillValue(indices[offset], chunk, idx);
    }

    public EntityId GetEntityId(long idx)
    {
        return result.GetEntityId(indices[idx]);
    }

    public AttributeId GetAttributeId(long idx)
    {
        return result.GetAttributeId(indices[idx]);
    }

    public TxId GetTransactionId(long idx)
    {
        return result.GetTransactionId(indices[idx]);
    }

    public ReadOnlySpan<byte> GetValue(long idx)
    {
        return result.GetValue(indices[idx]);
    }

    public ReadOnlyMemory<byte> GetValueMemory(long idx)
    {
        return result.GetValueMemory(indices[idx]);
    }

    public override string ToString()
    {
        return this.DatomResultToString();
    }
}
