using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// Provides a read-only view of a portion of an <see cref="IReadable"/>.
/// </summary>
public class ReadableView(IReadable inner, int offset, int length) : IReadable
{
    public IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
        {
            yield return inner[i + offset];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Length => length;
    public long DeepLength => length;

    public Datom this[int idx] => inner[idx + offset];

    public Datom LastDatom => inner[offset + length - 1];
    public EntityId GetEntityId(int idx)
    {
        return inner.GetEntityId(idx + offset);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return inner.GetAttributeId(idx + offset);
    }

    public TxId GetTransactionId(int idx)
    {
        return inner.GetTransactionId(idx + offset);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return inner.GetValue(idx + offset);
    }

    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }
}
