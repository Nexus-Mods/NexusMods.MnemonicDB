using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Columns.ULongColumns;
using NexusMods.EventSourcing.Storage.Nodes.Data;
using IReadable = NexusMods.EventSourcing.Abstractions.Nodes.Data.IReadable;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// Represents a sorted view of another node, this is most often used as a temporary view of a
/// node before it is merged into another node.
/// </summary>
public class SortedReadable : IReadable
{
    private readonly int[] _indexes;
    private readonly IReadable _inner;

    /// <summary>
    /// Represents a sorted view of another node, this is most often used as a temporary view of a
    /// node before it is merged into another node.
    /// </summary>
    /// <param name="indexes"></param>
    /// <param name="inner"></param>
    public SortedReadable(int[] indexes, IReadable inner)
    {
        _indexes = indexes;
        _inner = inner;
    }

    public IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < _indexes.Length; i++)
        {
            yield return _inner[_indexes[i]];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Length => _indexes.Length;
    public long DeepLength => _indexes.Length;

    public Datom this[int idx] => _inner[_indexes[idx]];

    public Datom LastDatom => _inner[_indexes[^1]];
    public EntityId GetEntityId(int idx)
    {
        return _inner.GetEntityId(_indexes[idx]);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return _inner.GetAttributeId(_indexes[idx]);
    }

    public TxId GetTransactionId(int idx)
    {
        return _inner.GetTransactionId(_indexes[idx]);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return _inner.GetValue(_indexes[idx]);
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
        public IEnumerator<ulong> GetEnumerator()
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

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable EntityIdsColumn => new SortedULongColumn(_indexes, _inner.EntityIdsColumn);
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable AttributeIdsColumn => new SortedULongColumn(_indexes, _inner.AttributeIdsColumn);
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable TransactionIdsColumn => new SortedULongColumn(_indexes, _inner.TransactionIdsColumn);
    public EventSourcing.Abstractions.Columns.BlobColumns.IReadable ValuesColumn => new SortedValueColumn(_indexes, _inner.ValuesColumn);

    public override string ToString()
    {
        return this.NodeToString();
    }
}
