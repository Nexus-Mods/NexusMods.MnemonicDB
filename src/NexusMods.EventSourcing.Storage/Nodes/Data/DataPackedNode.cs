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

    public long DeepLength { get; }

    public Datom this[int idx] => new()
    {
        E = GetEntityId(idx),
        A = GetAttributeId(idx),
        T = GetTransactionId(idx),
        V = Values.GetMemory(idx)
    };

    public Datom LastDatom { get; }
    public EntityId GetEntityId(int idx)
    {
        throw new NotImplementedException();
    }

    public AttributeId GetAttributeId(int idx)
    {
        throw new NotImplementedException();
    }

    public TxId GetTransactionId(int idx)
    {
        throw new NotImplementedException();
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        throw new NotImplementedException();
    }


    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }
}
