using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public partial class IndexNode
{
    protected void OnFlatSharpDeserialized()
    {

    }


    public Datom this[int idx] => throw new NotImplementedException();

    public Datom LastDatom { get; }
    public EntityId GetEntityId(int idx)
    {
        return EntityId.From(EntityIds[idx]);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return AttributeId.From(AttributeIds[idx]);
    }

    public TxId GetTransactionId(int idx)
    {
        return TxId.From(TransactionIds[idx]);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return Values[idx];
    }

    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

}
