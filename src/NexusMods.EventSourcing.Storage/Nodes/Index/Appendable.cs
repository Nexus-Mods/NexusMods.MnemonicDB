using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Index;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;
using NexusMods.EventSourcing.Storage.Nodes.Data;
using IAppendable = NexusMods.EventSourcing.Abstractions.Nodes.Index.IAppendable;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public class Appendable : IReadable, IAppendable
{
    public INodeStore Store { get; }

    private int _shallowLength;
    private long _deepLength;
    private readonly bool _isFrozen;

    private readonly Columns.ULongColumns.Appendable _entityIds;
    private readonly Columns.ULongColumns.Appendable _attributeIds;
    private readonly Columns.BlobColumns.Appendable _values;
    private readonly Columns.ULongColumns.Appendable _transactionIds;
    private readonly Columns.ULongColumns.Appendable _childCounts;
    private readonly Columns.ULongColumns.Appendable _childOffsets;

    private readonly Columns.ULongColumns.Appendable _children;
    private IDatomComparator _comparator = null!;

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
        _children = Columns.ULongColumns.Appendable.Create(initialSize);
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
        _children = Columns.ULongColumns.Appendable.Create(readable.ChildNodeIdsColumn, offset, childCount);

        var start = _childOffsets[0];
        for (var i = 0; i < childCount; i++)
        {
            _childOffsets[i] -= start;
        }

        Debug.Assert(_childOffsets[0] == 0, "First child offset should be 0");
        _deepLength = _childCounts.Sum(x => (long)x);
    }
    internal Appendable(IDatomComparator comparator, List<ExtensionMethods.ChildInfo> children)
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
        _children = Columns.ULongColumns.Appendable.Create();
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
        throw new NotImplementedException();
        //return new Appendable(comparator, [new Appendable(comparator, [new Data.Appendable()])]);
    }

    /// <summary>
    /// Creates a new index that is a subsection of the given readable. Think of this as a sub-view but a concrete one,
    /// the contents of the input node will be copied into the new node.
    /// </summary>
    public static Appendable Create(IReadable readable, int childOffset, int numberOfChildren)
    {
        return new Appendable(readable.Comparator, readable, childOffset, numberOfChildren);
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





    public IReadable PackIndex(INodeStore store)
    {
        List<ulong> childrenIds = new();
        List<EventSourcing.Abstractions.Nodes.Data.IReadable> children = new();

        foreach (var item in _children)
        {
            var child = item;
        TOP:
            switch (child)
            {
                case ReferenceNode node:
                    children.Add(child);
                    childrenIds.Add(node.StoreKey.Value);
                    break;
                case ReferenceIndexNode indexNode:
                    children.Add(child);
                    childrenIds.Add(indexNode.StoreKey.Value);
                    break;
                default:
                    // Pack it then re-evaluate it
                    child = child.Pack(store);
                    goto TOP;
            }
        }

        return new IndexPackedNode
        {
            SortOrder = (byte)_comparator.SortOrder,
            EntityIds = (ULongPackedColumn)_entityIds.Pack(),
            AttributeIds = (ULongPackedColumn)_attributeIds.Pack(),
            TransactionIds = (ULongPackedColumn)_transactionIds.Pack(),
            Values = (BlobPackedColumn)_values.Pack(),
            ChildCounts = (ULongPackedColumn)_childCounts.Pack(),
            ChildOffsets = (ULongPackedColumn)_childOffsets.Pack(),
            ChildNodes = childrenIds,
            Children = children,
            DeepLength = _deepLength,
            ShallowLength = _shallowLength
        };
    }

    public override string ToString()
    {
        return this.NodeToString();
    }
}
