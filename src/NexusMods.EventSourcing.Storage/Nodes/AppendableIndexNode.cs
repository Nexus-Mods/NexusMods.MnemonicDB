using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns.PackedColumns;
using NexusMods.EventSourcing.Storage.Algorithms;
using NexusMods.EventSourcing.Storage.Columns;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class AppendableIndexNode : IIndexNode
{
    private readonly UnsignedIntegerColumn<EntityId> _entityIds;
    private readonly UnsignedIntegerColumn<AttributeId> _attributeIds;
    private readonly UnsignedIntegerColumn<TxId> _transactionIds;
    private readonly UnsignedIntegerColumn<DatomFlags> _flags;
    private readonly AppendableBlobColumn _values;
    private readonly UnsignedIntegerColumn<int> _childCounts;

    private List<IDataNode> _children;
    private readonly IDatomComparator _comparator;
    private int _length;

    public AppendableIndexNode(IDatomComparator comparator)
    {
        _children = new List<IDataNode> { new AppendableNode() };
        _entityIds = new UnsignedIntegerColumn<EntityId>();
        _attributeIds = new UnsignedIntegerColumn<AttributeId>();
        _transactionIds = new UnsignedIntegerColumn<TxId>();
        _flags = new UnsignedIntegerColumn<DatomFlags>();
        _values = new AppendableBlobColumn();
        _childCounts = new UnsignedIntegerColumn<int>();
        _comparator = comparator;
        _length = 0;
    }

    private AppendableIndexNode(IDatomComparator comparator, List<IDataNode> newChildren) : this(comparator)
    {
        _children = newChildren;
        ReprocessChildren();
    }

    private AppendableIndexNode(IIndexNode indexNode)
    {
        _entityIds = UnsignedIntegerColumn<EntityId>.UnpackFrom(indexNode.EntityIds);
        _attributeIds = UnsignedIntegerColumn<AttributeId>.UnpackFrom(indexNode.AttributeIds);
        _transactionIds = UnsignedIntegerColumn<TxId>.UnpackFrom(indexNode.TransactionIds);
        _flags = UnsignedIntegerColumn<DatomFlags>.UnpackFrom(indexNode.Flags);
        _values = AppendableBlobColumn.UnpackFrom(indexNode.Values);
        _childCounts = UnsignedIntegerColumn<int>.UnpackFrom(indexNode.ChildCounts);
        _children = indexNode.Children.ToList();
        _comparator = indexNode.Comparator;
        _length = indexNode.Length;
    }

    private void ReprocessChildren()
    {
        // Skip storing the last datom of the last block as we never use it and always use Datom.Max
        var butLast = _children.Take(_children.Count - 1).ToArray();
        _entityIds.Initialize(butLast.Select(c => c.LastDatom.E));
        _attributeIds.Initialize(butLast.Select(c => c.LastDatom.A));
        _transactionIds.Initialize(butLast.Select(c => c.LastDatom.T));
        _flags.Initialize(butLast.Select(c => c.LastDatom.F));
        _values.Initialize(butLast.Select(c => c.LastDatom.V));
        _childCounts.Initialize(_children.Select(c => c.Length));
        _length = _childCounts.Sum();
    }


    public int Length => _length;
    public IColumn<EntityId> EntityIds => _entityIds;
    public IColumn<AttributeId> AttributeIds => _attributeIds;
    public IColumn<TxId> TransactionIds => _transactionIds;
    public IColumn<DatomFlags> Flags => _flags;

    public IBlobColumn Values => _values;

    public Datom this[int idx]
    {
        get
        {
            var acc = 0;
            for (var j = 0; j < _children.Count; j++)
            {
                var childSize = _childCounts[idx];
                if (idx < acc + childSize)
                {
                    return _children[idx][idx - acc];
                }
                acc += childSize;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public Datom LastDatom =>
        new()
        {
            E = _entityIds[_length - 1],
            A = _attributeIds[_length - 1],
            T = _transactionIds[_length - 1],
            F = _flags[_length - 1],
            V = _values[_length - 1]
        };

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new System.NotImplementedException();
    }

    public IDataNode Flush(INodeStore store)
    {
        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (child is AppendableNode appendableChunk)
            {
                var packedChild = appendableChunk.Pack();
                _children[i] = store.Flush(packedChild);
            }
        }
        var packed = Pack();
        return store.Flush(packed);
    }

    private IDataNode Pack()
    {
        var length = _length;
        var entityIds = _entityIds.Pack();
        var attributeIds = _attributeIds.Pack();
        var transactionIds = _transactionIds.Pack();
        var flags = _flags.Pack();
        var values = _values.Pack();

        return new PackedIndexNode(length, entityIds, attributeIds, transactionIds, flags, values, _childCounts.Pack(), _comparator, _children);
    }

    public IEnumerable<IDataNode> Children => _children;
    public IColumn<int> ChildCounts => _childCounts;
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

    private IEnumerable<(Datom Datom, int Index)> ChildMarkers()
    {
        var length = _entityIds.Length;
        for (var i = 0; i < length; i++)
        {
            var datom = new Datom
            {
                E = _entityIds[i],
                A = _attributeIds[i],
                T = _transactionIds[i],
                F = _flags[i],
                V = _values[i]
            };
            yield return (datom, i);
        }

        yield return (Datom.Max, _children.Count - 1);
    }

    public AppendableIndexNode Ingest(IDataNode node)
    {
        var start = 0;

        var newChildren = new List<IDataNode>(_children.Count);

        void MaybeSplit(AppendableNode chunk)
        {
            if (chunk.Length > Configuration.DataBlockSize * 2)
            {
                var (a, b) = chunk.Split();
                MaybeSplit(a);
                MaybeSplit(b);
            }
            else
            {
                newChildren.Add(chunk);
            }

        }


        foreach (var (lastDatom, idx) in ChildMarkers())
        {
            var last = BinarySearch.SeekEqualOrLess(node, _comparator, start, node.Length, lastDatom);
            if (last < node.Length)
            {
                var newNode = Merge(_children[idx], node.Range(start, last));
                MaybeSplit(newNode);
                start = last;
            }
            else if (last == node.Length)
            {
                var newNode = Merge(_children[idx], node.Range(start, last));
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

        return new AppendableIndexNode(_comparator, newChildren);

    }

    public AppendableNode Merge<TChunk, TEnumerable>(TChunk child, TEnumerable datoms)
    where TChunk : IDataNode
    where TEnumerable : IEnumerable<Datom>
    {
        return AppendableNode.Initialize(child.Merge(datoms, _comparator));
    }

    public static AppendableIndexNode UnpackFrom(IIndexNode indexNode)
    {
        return new AppendableIndexNode(indexNode);
    }
}
