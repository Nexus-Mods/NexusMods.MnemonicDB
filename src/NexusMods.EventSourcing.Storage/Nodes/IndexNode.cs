using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.ValueTypes;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class IndexNode : INode
{
    private readonly List<INode> _children;
    private readonly Configuration _configuration;

    struct Child
    {
        public OnHeapDatom LastDatom;
        public INode Node;
    }

    public IndexNode(Configuration configuration)
    {
        _configuration = configuration;
        _children = new List<INode>();
    }

    public IndexNode(INode toSplit, Configuration configuration)
    {
        _configuration = configuration;
        var (a, b) = toSplit.Split();
        _children = new List<INode>
        {
            a, b
        };
    }



    private void InsertNode(INode node, int index)
    {
        _children.Insert(index, node);
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
                var newChild = newNode._children[childIndex].Ingest<TIterator, TDatom, IRawDatom, TComparator>(other, newNode._children[childIndex].LastDatom, comparator);
                newNode.ReplaceNode(newChild, childIndex);
            }
            else
            {
                var newChild = newNode._children[^1].Ingest<TIterator, TDatom, IRawDatom, TComparator>(other, OnHeapDatom.Max, comparator);
                newNode.ReplaceNode(newChild, childIndex);
            }

        }

        return newNode;
    }

    private IndexNode Clone()
    {
        var clone = new IndexNode(_configuration);
        clone._children.AddRange(_children);
        return clone;
    }

    private void ReplaceNode(INode node, int index)
    {
        _children[index] = node;
        MaybeSplit(index);
    }

    private void MaybeSplit(int index)
    {
        var node = _children[index];

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
            _children.InsertRange(index, InnerSplit(node));
        }
    }

    private int FindIndex<TDatomComparator>(in TDatomComparator comparator, in IRawDatom datom)
        where TDatomComparator : IDatomComparator
    {
        throw new NotImplementedException();
        /*
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
        */
    }
    public int Count => _children.Sum(c => c.Count);
    public int ChildCount => _children.Count;

    public IRawDatom LastDatom => _children[^1].LastDatom;

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
                if (current + child.Count > idx)
                {
                    return child[idx - current];
                }
                current += child.Count;
            }
            throw new System.IndexOutOfRangeException();
        }
    }

    public INode Flush(NodeStore store)
    {
        for(var idx = 0; idx < _children.Count; idx++)
        {
            _children[idx] = _children[idx].Flush(store);
        }
        return store.Flush(this);
    }

        public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(4);
        MemoryMarshal.Write(span, (ushort)NodeVersions.IndexNode);
        MemoryMarshal.Write(span.SliceFast(2), (ushort)_children.Count);
        writer.Advance(4);

        var entityIds = new List<ulong>();
        var attributeIds = new List<ushort>();
        var txIds = new List<ulong>();
        var flags = new List<DatomFlags>();
        var valueLiterals = new List<ulong>();
        var refIds = new List<StoreKey>();
        var counts = new List<int>();
        var childCounts = new List<int>();

        var valueBlob = new PooledMemoryBufferWriter(8);

        foreach (var child in _children)
        {
            var reference = child as ReferenceNode;
            Debug.Assert(reference != null, "All children of an index node should be reference nodes before writing to disk.");
            entityIds.Add(reference.LastDatom.EntityId);
            attributeIds.Add(reference.LastDatom.AttributeId);
            txIds.Add(reference.LastDatom.TxId);
            flags.Add(reference.LastDatom.Flags);

            var flag = reference.LastDatom.Flags;
            if (flag.HasFlag(DatomFlags.InlinedData))
            {
                valueLiterals.Add(reference.LastDatom.ValueLiteral);
            }
            else
            {
                var offset = valueBlob.GetWrittenSpan().Length;
                var valueSpan = valueBlob.GetSpan(reference.LastDatom.ValueSpan.Length);
                reference.LastDatom.ValueSpan.CopyTo(valueSpan);
                valueBlob.Advance(reference.LastDatom.ValueSpan.Length);
                valueLiterals.Add((ulong)offset << 32 | (uint)reference.LastDatom.ValueSpan.Length);
            }

            refIds.Add(reference.Id);
            counts.Add(reference.Count);
            childCounts.Add(reference.ChildCount);
        }

        writer.WriteList(entityIds);
        writer.WriteList(attributeIds);
        writer.WriteList(txIds);
        writer.WriteList(flags);
        writer.WriteList(valueLiterals);
        writer.WriteList(refIds);
        writer.WriteList(counts);
        writer.WriteList(childCounts);

        writer.Write(valueBlob.GetWrittenSpan());
    }

    public void InitializeFrom(NodeStore store, ReadOnlySpan<byte> value)
    {
        var version = MemoryMarshal.Read<ushort>(value);
        Debug.Assert(version == (ushort)NodeVersions.IndexNode, "Invalid node version");

        var childCount = MemoryMarshal.Read<ushort>(value.Slice(2));

        var dataOffset = value.SliceFast(4);

        dataOffset = dataOffset.ReadNewList<ulong>(out var entityIds, childCount);
        dataOffset = dataOffset.ReadNewList<ushort>(out var attributeIds, childCount);
        dataOffset = dataOffset.ReadNewList<ulong>(out var txIds, childCount);
        dataOffset = dataOffset.ReadNewList<DatomFlags>(out var flags, childCount);
        dataOffset = dataOffset.ReadNewList<ulong>(out var valueLiterals, childCount);
        dataOffset = dataOffset.ReadNewList<StoreKey>(out var refIds, childCount);
        dataOffset = dataOffset.ReadNewList<int>(out var counts, childCount);
        dataOffset = dataOffset.ReadNewList<int>(out var childCounts, childCount);


        for (var idx = 0; idx < childCount; idx++)
        {
            ulong inlineData = valueLiterals[idx];
            byte[] valueData = Array.Empty<byte>();
            if (!flags[idx].HasFlag(DatomFlags.InlinedData))
            {
                inlineData = valueLiterals[idx] >> 32;
                var length = (int)(valueLiterals[idx] & 0xFFFFFFFF);
                valueData = dataOffset.SliceFast((int)inlineData, length).ToArray();
                inlineData = 0;
            }

            var reference = new ReferenceNode(store)
            {
                Id = refIds[idx],
                Count = counts[idx],
                ChildCount = childCounts[idx],
                LastDatom = new OnHeapDatom
                {
                    EntityId = entityIds[idx],
                    AttributeId = attributeIds[idx],
                    TxId = txIds[idx],
                    Flags = flags[idx],
                    ValueLiteral = inlineData,
                    ValueData = valueData
                }
            };
            _children.Add(reference);
        }
    }
}
