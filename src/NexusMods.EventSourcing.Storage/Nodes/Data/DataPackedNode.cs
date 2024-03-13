using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public partial class DataPackedNode : IPacked
{
    public IEnumerator<Datom> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public long DeepLength => Length;

    public Datom this[int idx] => new()
    {
        E = GetEntityId(idx),
        A = GetAttributeId(idx),
        T = GetTransactionId(idx),
        V = Values.GetMemory(idx)
    };

    public Datom LastDatom => this[Length - 1];
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
