﻿using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class IndexNode : INode
{
    private readonly List<Child> _children;
    private readonly Configuration _configuration;

    struct Child
    {
        public OnHeapDatom LastDatom;
        public INode Node;
    }

    public IndexNode()
    {
        _configuration = Configuration.Default;
        _children = new List<Child>();
    }

    public IndexNode(INode toSplit, Configuration configuration)
    {
        _configuration = configuration;
        var (a, b) = toSplit.Split();
        _children = new List<Child>
        {
            new() { Node = a, LastDatom = OnHeapDatom.Create(a.LastDatom) },
            new() { Node = b, LastDatom = OnHeapDatom.Create(b.LastDatom) }
        };
    }

    private void InsertNode(INode node, int index)
    {
        _children.Insert(index, new Child { Node = node, LastDatom = OnHeapDatom.Create(node.LastDatom) });
    }

    public INode Insert<TInput, TDatomComparator>(in TInput inputDatom, in TDatomComparator comparator) where TInput : IRawDatom where TDatomComparator : IDatomComparator
    {
        throw new System.NotImplementedException();
    }

    public INode Ingest<TIterator, TDatom, TDatomStop, TComparator>(in TIterator other, in TDatomStop stopDatom,
        TComparator comparator) where TIterator : IIterator<TDatom> where TDatom : IRawDatom where TDatomStop : IRawDatom where TComparator : IDatomComparator
    {
        var newNode = Clone();

        while(!other.AtEnd)
        {
            TDatom datom;
            if (!other.Value(out datom))
            {
                if (other.AtEnd)
                    break;
                other.Next();
                continue;
            }

            var childIndex = newNode.FindIndex(comparator, datom);
            if (childIndex != newNode._children.Count - 1)
            {
                var newChild = newNode._children[childIndex].Node.Ingest<TIterator, TDatom, OnHeapDatom, TComparator>(other, newNode._children[childIndex].LastDatom, comparator);
                newNode.ReplaceNode(newChild, childIndex);
            }
            else
            {
                var newChild = newNode._children[^1].Node.Ingest<TIterator, TDatom, OnHeapDatom, TComparator>(other, OnHeapDatom.Max, comparator);
                newNode.ReplaceNode(newChild, childIndex);
            }

        }

        return newNode;
    }

    private IndexNode Clone()
    {
        var clone = new IndexNode();
        clone._children.AddRange(_children);
        return clone;
    }

    private void ReplaceNode(INode node, int index)
    {
        _children[index] = new Child { Node = node, LastDatom = OnHeapDatom.Create(node.LastDatom) };
        MaybeSplit(index);
    }

    private void MaybeSplit(int index)
    {
        var node = _children[index].Node;

        IEnumerable<INode> InnerSplit(INode node)
        {
            var (a, b) = node.Split();

            if (a.SizeState == SizeStates.OverSized)
                foreach (var itm in InnerSplit(a))
                    yield return itm;
            else
                yield return a;

            if (b.SizeState == SizeStates.OverSized)
                foreach (var itm in InnerSplit(b))
                    yield return itm;
            else
                yield return b;
        }

        if (node.SizeState == SizeStates.OverSized)
        {
            _children.RemoveAt(index);
            _children.InsertRange(index, InnerSplit(node).Select(n => new Child { Node = n, LastDatom = OnHeapDatom.Create(n.LastDatom) }));
        }
    }

    private int FindIndex<TDatomComparator>(in TDatomComparator comparator, in IRawDatom datom)
        where TDatomComparator : IDatomComparator
    {
        var start = 0;
        var end = _children.Count - 1;

        while (start <= end)
        {
            var mid = start + (end - start) / 2;
            if (comparator.Compare(_children[mid].LastDatom, datom) < 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid - 1;
            }
        }

        return start;
    }
    public int Count => _children.Sum(c => c.Node.Count);
    public int ChildCount => _children.Count;

    public IRawDatom LastDatom => throw new System.NotImplementedException();

    public (INode, INode) Split()
    {
        throw new System.NotImplementedException();
    }

    public SizeStates SizeState
    {
        get
        {
            if (_children.Count > _configuration.IndexBlockSize * 2)
            {
                return SizeStates.OverSized;
            }
            if (_children.Count < _configuration.IndexBlockSize / 2)
            {
                return SizeStates.UnderSized;
            }
            return SizeStates.Ok;
        }
    }

    public IRawDatom this[int idx]
    {
        get {
            var current = 0;
            foreach (var child in _children)
            {
                if (current + child.Node.Count > idx)
                {
                    return child.Node[idx - current];
                }
                current += child.Node.Count;
            }
            throw new System.IndexOutOfRangeException();
        }
    }
}
