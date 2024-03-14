using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Index;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public class Appendable : IReadable, IAppendable
{
    private readonly int _length;
    private readonly bool _isFrozen;

    private readonly Columns.ULongColumns.Appendable _entityIds;
    private readonly Columns.ULongColumns.Appendable _attributeIds;
    private readonly Columns.BlobColumns.Appendable _values;
    private readonly Columns.ULongColumns.Appendable _transactionIds;
    private readonly Columns.ULongColumns.Appendable _childCounts;
    private readonly Columns.ULongColumns.Appendable _childOffsets;

    private List<EventSourcing.Abstractions.Nodes.Data.IReadable> _children;
    private readonly IDatomComparator _comparator;

    public Appendable(IDatomComparator comparator, int initialSize = Columns.ULongColumns.Appendable.DefaultSize)
    {
        _comparator = comparator;
        _isFrozen = false;
        _length = 0;
        _entityIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _attributeIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _transactionIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _values = Columns.BlobColumns.Appendable.Create(initialSize);
        _childCounts = Columns.ULongColumns.Appendable.Create(initialSize);
        _childOffsets = Columns.ULongColumns.Appendable.Create(initialSize);
        _children = new List<EventSourcing.Abstractions.Nodes.Data.IReadable> { new Data.Appendable() };
    }

    private Appendable(IDatomComparator comparator, List<EventSourcing.Abstractions.Nodes.Data.IReadable> newChildren)
        : this(comparator)
    {
        _children = newChildren;
        ReprocessChildren();
    }

    private void ReprocessChildren()
    {
        throw new NotImplementedException();
    }


    public int Length => _length;
    public long DeepLength => _childCounts.Sum(c => (long)c);

    public Datom this[int idx]
    {
        get
        {
            var acc = 0;
            for (var i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                var childLength = child.Length;
                if (acc + childLength > idx)
                {
                    return child[idx - acc];
                }

                acc += childLength;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public Datom LastDatom => new()
    {
        E = EntityId.From(_entityIds[_length - 1]),
        A = AttributeId.From(_attributeIds[_length - 1]),
        T = TxId.From(_transactionIds[_length - 1]),
        V = _values.GetMemory(_length - 1)
    };

    /// <summary>
    /// Gets the last datom in each child node, except for the last which is always
    /// Datom.Max.
    /// </summary>
    private IEnumerable<(Datom Datom, int Index)> ChildMarkers()
    {
        var length = _entityIds.Length;
        for (var i = 0; i < length; i++)
        {
            var datom = new Datom
            {
                E = EntityId.From(_entityIds[i]),
                A = AttributeId.From(_attributeIds[i]),
                T = TxId.From(_transactionIds[i]),
                V = _values.GetMemory(i)
            };
            yield return (datom, i);
        }

        yield return (Datom.Max, _children.Count - 1);
    }

    public EntityId GetEntityId(int idx)
    {
        return EntityId.From(_entityIds[idx]);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return AttributeId.From(_attributeIds[idx]);
    }

    public TxId GetTransactionId(int idx)
    {
        return TxId.From(_transactionIds[idx]);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return _values[idx];
    }

    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable EntityIdsColumn => _entityIds;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable AttributeIdsColumn => _attributeIds;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable TransactionIdsColumn => _transactionIds;
    public EventSourcing.Abstractions.Columns.BlobColumns.IReadable ValuesColumn => _values;

    public long GetChildCount(int idx)
    {
        return (long)_childCounts[idx];
    }

    public long GetChildOffset(int idx)
    {
        return (long)_childOffsets[idx];
    }

    EventSourcing.Abstractions.Nodes.Data.IReadable IReadable.GetChild(int idx)
    {
        return _children[idx];
    }

    public IEnumerator<Datom> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IAppendable Ingest(EventSourcing.Abstractions.Nodes.Data.IReadable node)
    {
        var start = 0;

        var newChildren = new List<EventSourcing.Abstractions.Nodes.Data.IReadable>(_children.Count);

        void MaybeSplit(EventSourcing.Abstractions.Nodes.Data.IReadable node)
        {
            if (node.Length > Configuration.DataBlockSize * 2)
            {
                var splits = node.Split(Configuration.DataBlockSize);
                foreach (var split in splits)
                    MaybeSplit(split);
            }
            else
            {
                newChildren.Add(node);
            }

        }

        foreach (var (lastDatom, idx) in ChildMarkers())
        {
            var last = node.Find(lastDatom, _comparator);
            if (last < node.Length)
            {
                var newNode = _children[idx].Merge(node.SubView(start, last));
                MaybeSplit(newNode);
                start = last;
            }
            else if (last == node.Length)
            {
                var newNode = _children[idx].Merge(node.SubView(start, last));
                MaybeSplit(newNode);

                // Add the remaining children nodes
                for (var j = idx + 1; j < _children.Count; j++)
                {
                    newChildren.Add(_children[j]);
                }

                break;
            }
            else
            {
                newChildren.Add(_children[idx]);
            }
        }

        return new Appendable(_comparator, newChildren);
    }
}
