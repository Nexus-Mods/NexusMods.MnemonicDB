using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

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

    internal class ReadableViewBlobColumn(int offset, int length, EventSourcing.Abstractions.Columns.BlobColumns.IReadable inner)
      : EventSourcing.Abstractions.Columns.BlobColumns.IReadable
    {
        public int Count => length;

        public ReadOnlySpan<byte> this[int idx] => inner[idx + offset];

        public ReadOnlyMemory<byte> Memory => inner.Memory;
        public EventSourcing.Abstractions.Columns.ULongColumns.IReadable LengthsColumn => new ReadableViewULongColumn(offset, length, inner.LengthsColumn);
        public EventSourcing.Abstractions.Columns.ULongColumns.IReadable OffsetsColumn => new ReadableViewULongColumn(offset, length, inner.OffsetsColumn);
    }

    internal class ReadableViewULongColumn(int offset, int length, EventSourcing.Abstractions.Columns.ULongColumns.IReadable inner)
      : EventSourcing.Abstractions.Columns.ULongColumns.IReadable
    {
        public int Length => length;
        public void CopyTo(int innerOffset, Span<ulong> dest)
        {
            for (var i = 0; i < dest.Length; i++)
            {
                dest[i] = inner[i + offset + innerOffset];
            }
        }
        public ulong this[int idx] => inner[idx + offset];
        public IEnumerator<ulong> GetEnumerator()
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
    }



    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable EntityIdsColumn => new ReadableViewULongColumn(offset, length, inner.EntityIdsColumn);
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable AttributeIdsColumn => new ReadableViewULongColumn(offset, length, inner.AttributeIdsColumn);
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable TransactionIdsColumn => new ReadableViewULongColumn(offset, length, inner.TransactionIdsColumn);
    public EventSourcing.Abstractions.Columns.BlobColumns.IReadable ValuesColumn => new ReadableViewBlobColumn(offset, length, inner.ValuesColumn);

    public override string ToString()
    {
        return this.NodeToString();
    }
}
