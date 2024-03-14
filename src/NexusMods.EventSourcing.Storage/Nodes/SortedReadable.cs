using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Columns.ULongColumns;
using IReadable = NexusMods.EventSourcing.Abstractions.Nodes.Data.IReadable;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// Represents a sorted view of another node, this is most often used as a temporary view of a
/// node before it is merged into another node.
/// </summary>
/// <param name="indexes"></param>
/// <param name="inner"></param>
public class SortedReadable(int[] indexes, IReadable inner) : IReadable
{
    public IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < indexes.Length; i++)
        {
            yield return inner[indexes[i]];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Length => indexes.Length;
    public long DeepLength => indexes.Length;

    public Datom this[int idx] => inner[indexes[idx]];

    public Datom LastDatom => inner[indexes[^1]];
    public EntityId GetEntityId(int idx)
    {
        return inner.GetEntityId(indexes[idx]);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return inner.GetAttributeId(indexes[idx]);
    }

    public TxId GetTransactionId(int idx)
    {
        return inner.GetTransactionId(indexes[idx]);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return inner.GetValue(indexes[idx]);
    }

    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

    private class SortedULongColumn(int[] indexes, EventSourcing.Abstractions.Columns.ULongColumns.IReadable inner)
        : EventSourcing.Abstractions.Columns.ULongColumns.IReadable
    {
        public int Length => indexes.Length;
        public void CopyTo(int offset, Span<ulong> dest)
        {
            for (var i = 0; i < dest.Length; i++)
            {
                dest[i] = inner[indexes[i + offset]];
            }
        }

        public ulong this[int idx] => inner[indexes[idx]];
    }

    private class SortedValueColumn(int[] indexes, EventSourcing.Abstractions.Columns.BlobColumns.IReadable inner)
        : EventSourcing.Abstractions.Columns.BlobColumns.IReadable
    {
        public int Count => indexes.Length;

        public ReadOnlySpan<byte> this[int idx] => inner[indexes[idx]];

        public ReadOnlyMemory<byte> Memory => inner.Memory;
        public EventSourcing.Abstractions.Columns.ULongColumns.IReadable LengthsColumn => new SortedULongColumn(indexes, inner.LengthsColumn);
        public EventSourcing.Abstractions.Columns.ULongColumns.IReadable OffsetsColumn => new SortedULongColumn(indexes, inner.OffsetsColumn);
    }

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable EntityIdsColumn => new SortedULongColumn(indexes, inner.EntityIdsColumn);
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable AttributeIdsColumn => new SortedULongColumn(indexes, inner.AttributeIdsColumn);
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable TransactionIdsColumn => new SortedULongColumn(indexes, inner.TransactionIdsColumn);
    public EventSourcing.Abstractions.Columns.BlobColumns.IReadable ValuesColumn => new SortedValueColumn(indexes, inner.ValuesColumn);
}
