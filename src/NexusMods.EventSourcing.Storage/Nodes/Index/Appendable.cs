using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Index;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public class Appendable : IReadable, IAppendable
{
    private int _shallowLength;
    private long _deepLength;
    private readonly bool _isFrozen;

    private readonly Columns.ULongColumns.Appendable _entityIds;
    private readonly Columns.ULongColumns.Appendable _attributeIds;
    private readonly Columns.BlobColumns.Appendable _values;
    private readonly Columns.ULongColumns.Appendable _transactionIds;
    private readonly Columns.ULongColumns.Appendable _childCounts;
    private readonly Columns.ULongColumns.Appendable _childOffsets;

    private List<EventSourcing.Abstractions.Nodes.Data.IReadable> _children;
    private readonly IDatomComparator _comparator;

    private Appendable(IDatomComparator comparator, int initialSize = Columns.ULongColumns.Appendable.DefaultSize)
    {
        _comparator = comparator;
        _isFrozen = false;
        _shallowLength = 0;
        _entityIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _attributeIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _transactionIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _values = Columns.BlobColumns.Appendable.Create(initialSize);
        _childCounts = Columns.ULongColumns.Appendable.Create(initialSize);
        _childOffsets = Columns.ULongColumns.Appendable.Create(initialSize);
        _children = new List<EventSourcing.Abstractions.Nodes.Data.IReadable> { new Data.Appendable() };
    }

    private Appendable(IDatomComparator comparator, IReadable readable, int offset, int childCount)
    {
        _comparator = comparator;
        _isFrozen = false;
        _shallowLength = childCount;

        _entityIds = Columns.ULongColumns.Appendable.Create(readable.EntityIdsColumn, offset, childCount);
        _attributeIds = Columns.ULongColumns.Appendable.Create(readable.AttributeIdsColumn, offset, childCount);
        _transactionIds = Columns.ULongColumns.Appendable.Create(readable.TransactionIdsColumn, offset, childCount);
        _values = Columns.BlobColumns.Appendable.Create(readable.ValuesColumn, offset, childCount);
        _childCounts = Columns.ULongColumns.Appendable.Create(readable.ChildCountsColumn, offset, childCount);
        _childOffsets = Columns.ULongColumns.Appendable.Create(readable.ChildOffsetsColumn, offset, childCount);
        _children = new List<EventSourcing.Abstractions.Nodes.Data.IReadable>();
        for (var i = 0; i < childCount; i++)
        {
            _children.Add(readable.GetChild(offset + i));
        }

        var start = _childOffsets[0];
        for (var i = 0; i < childCount; i++)
        {
            _childOffsets[i] -= start;
        }

        Debug.Assert(_childOffsets[0] == 0, "First child offset should be 0");
        _deepLength = _children.Sum(c => c.DeepLength);
    }

    private Appendable(IDatomComparator comparator, List<EventSourcing.Abstractions.Nodes.Data.IReadable> newChildren)
        : this(comparator)
    {
        _children = newChildren;
        ReprocessChildren();
    }

    private Appendable(IDatomComparator comparator, List<ChildInfo> children)
    {
        _comparator = comparator;
        _isFrozen = false;
        _shallowLength = children.Count;
        _entityIds = Columns.ULongColumns.Appendable.Create();
        _attributeIds = Columns.ULongColumns.Appendable.Create();
        _transactionIds = Columns.ULongColumns.Appendable.Create();
        _values = Columns.BlobColumns.Appendable.Create();
        _childCounts = Columns.ULongColumns.Appendable.Create();
        _childOffsets = Columns.ULongColumns.Appendable.Create();
        _children = new List<EventSourcing.Abstractions.Nodes.Data.IReadable>();
        _deepLength = 0;

        foreach (var child in children)
        {
            _entityIds.Append(child.LastDatom.E.Value);
            _attributeIds.Append(child.LastDatom.A.Value);
            _transactionIds.Append(child.LastDatom.T.Value);
            _values.Append(child.LastDatom.V.Span);
            _childCounts.Append(child.DeepLength);
            _childOffsets.Append((ulong)_deepLength);
            _deepLength += (long)child.DeepLength;
            _children.Add(child.Child);
        }
        Debug.Assert(_childOffsets[0] == 0, "First child offset should be 0");

    }

    /// <summary>
    /// Creates an index node for a completely new tree, internally it nests two index nodes and places a data node
    /// at the third level. This removes any need for top-level logic of splitting of nodes, as the top most node
    /// will just continue to expand forever. With a proper branching rate this should be a non-issue. As it allows
    /// for BranchFactor ^ 3 nodes in total.
    /// </summary>
    public static Appendable Create(IDatomComparator comparator)
    {
        return new Appendable(comparator, [new Appendable(comparator, [new Data.Appendable()])]);
    }

    /// <summary>
    /// Creates a new index that is a subsection of the given readable. Think of this as a sub-view but a concrete one,
    /// the contents of the input node will be copied into the new node.
    /// </summary>
    public static Appendable Create(IReadable readable, int childOffset, int numberOfChildren)
    {
        return new Appendable(readable.Comparator, readable, childOffset, numberOfChildren);
    }

    private void ReprocessChildren()
    {
        var offset = 0UL;
        for (var idx = 0; idx < _children.Count; idx++)
        {
            var child = _children[idx];
            var lastDatom = child.Length == 0 ? Datom.Max : child.LastDatom;
            _entityIds.Append(lastDatom.E.Value);
            _attributeIds.Append(lastDatom.A.Value);
            _transactionIds.Append(lastDatom.T.Value);
            _values.Append(lastDatom.V.Span);
            _childCounts.Append((ulong)child.DeepLength);
            _childOffsets.Append(offset);
            offset += (ulong)child.DeepLength;
            _shallowLength++;
            _deepLength += child.DeepLength;
        }
    }


    public int Length => _shallowLength;
    public long DeepLength => _deepLength;

    public Datom this[int idx]
    {
        get
        {
            for (var i = 0; i < _childOffsets.Length; i++)
            {
                var offset = (int)_childOffsets[i];
                var childLength = (int)_childCounts[i];
                if (idx >= offset && idx < offset + childLength)
                {
                    return _children[i][idx - offset];
                }
            }
            throw new IndexOutOfRangeException();
        }
    }

    public Datom LastDatom => new()
    {
        E = EntityId.From(_entityIds[_shallowLength - 1]),
        A = AttributeId.From(_attributeIds[_shallowLength - 1]),
        T = TxId.From(_transactionIds[_shallowLength - 1]),
        V = _values.GetMemory(_shallowLength - 1)
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

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildCountsColumn => _childCounts;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildOffsetsColumn => _childOffsets;
    public IDatomComparator Comparator => _comparator;

    public IEnumerator<Datom> GetEnumerator()
    {
        foreach (var child in _children)
        {
            foreach (var datom in child)
            {
                yield return datom;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    private record struct ChildInfo(Datom LastDatom, EventSourcing.Abstractions.Nodes.Data.IReadable Child, ulong DeepLength, int ShallowLength);


    static void MaybeSplit(EventSourcing.Abstractions.Nodes.Data.IReadable src, List<ChildInfo> children)
    {
        if (src is IAppendable index && index.Length > Configuration.IndexBlockSize)
        {
            var split = index.Split(Configuration.IndexBlockSize).ToArray();
            foreach (var child in split)
            {
                children.Add(new ChildInfo(child.LastDatom, child, (ulong)child.DeepLength, child.Length));
            }
        }
        else if (src is EventSourcing.Abstractions.Nodes.Data.IReadable data &&
                 data.Length > Configuration.DataBlockSize)
        {
            var split = data.Split(Configuration.DataBlockSize).ToArray();
            foreach (var child in split)
            {
                children.Add(new ChildInfo(child.LastDatom, child, (ulong)child.DeepLength, child.Length));
            }
        }
        else
        {
            children.Add(new ChildInfo(src.LastDatom, src, (ulong)src.DeepLength, src.Length));
        }

    }

    public IAppendable Ingest(EventSourcing.Abstractions.Nodes.Data.IReadable node)
    {
        var start = 0;

        var children = new List<ChildInfo>();



        for (int idx = 0; idx < _children.Count; idx++)
        {
            var datom = idx == _children.Count - 1 ? Datom.Max : _children[idx].LastDatom;
            var last = node.Find(datom, _comparator);

            if (last < node.Length)
            {
                var newNode = _children[idx].Merge(node.SubView(start, last - start), _comparator);
                MaybeSplit(newNode, children);
                start = last;
            }
            else if (last == node.Length)
            {
                var newNode = _children[idx].Merge(node.SubView(start, last - start), _comparator);
                MaybeSplit(newNode, children);

                for (int i = idx + 1; i < _children.Count; i++)
                {
                    children.Add(new ChildInfo(_children[i].LastDatom, _children[i], (ulong)_children[i].DeepLength, _children[i].Length));
                }

                break;
            }
        }



        return new Appendable(_comparator, children);
    }

    public override string ToString()
    {
        return this.NodeToString();
    }
}
