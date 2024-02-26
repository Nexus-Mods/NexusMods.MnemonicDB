using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// An editable block of datoms that can be sorted and written to a buffer.
/// </summary>
public class OldAppendableNode(Configuration config) : INode,
    IIterable<OldAppendableNode.FlyweightIterator, OldAppendableNode.FlyweightRawDatom>
{
    private readonly PooledMemoryBufferWriter _pooledMemoryBufferWriter = new();
    private readonly List<ulong> _entityIds = new();
    private readonly List<ushort> _attributeIds = new();
    private readonly List<ulong> _txIds = new();
    private readonly List<DatomFlags> _flags = new();
    private readonly List<ulong> _values = new();
    public int Count => _entityIds.Count;
    public int ChildCount => _entityIds.Count;

    public IRawDatom LastDatom => new FlyweightRawDatom(this, (uint)Count - 1);

    public (INode, INode) Split()
    {
        var a = new OldAppendableNode(config);
        var b = new OldAppendableNode(config);
        var half = Count / 2;

        var baseIterator = Iterate();


        for (var i = 0; i < half; i++)
        {
            baseIterator.Value(out var datom);
            a.Append(datom);
            baseIterator.Next();
        }

        for (var i = half; i < Count; i++)
        {
            baseIterator.Value(out var datom);
            b.Append(datom);
            baseIterator.Next();
        }


        return (a, b);
    }

    public SizeStates SizeState
    {
        get
        {
            if (Count < config.DataBlockSize / 2)
                return SizeStates.UnderSized;
            if (Count > config.DataBlockSize * 2)
                return SizeStates.OverSized;
            return SizeStates.Ok;
        }
    }

    public void Append<TRawDatom>(in TRawDatom datom)
    where TRawDatom : IRawDatom
    {
        _entityIds.Add(datom.EntityId);
        _attributeIds.Add(datom.AttributeId);
        _txIds.Add(datom.TxId);
        _flags.Add(datom.Flags);
        _values.Add(datom.ValueLiteral);
        if (datom.Flags.HasFlag(DatomFlags.InlinedData))
            return;

        var span = datom.ValueSpan;
        var offset = _pooledMemoryBufferWriter.GetWrittenSpan().Length;
        _values.Add((ulong)((offset << 4) | span.Length));
        _pooledMemoryBufferWriter.Write(span);
    }

    public void Sort<TComparer>(TComparer comparer)
        where TComparer : IDatomComparator
    {
        // Simple Quicksort implementation, this could be replaced with something better
        // but we don't sort often, and we need to be able to swap out all the values associated
        // with a datom, so we can't use a simple array of structs.
        Sort(comparer, 0, Count - 1);
    }

    private void Sort<TComparer>(TComparer comparer, int left, int right)
    where TComparer : IDatomComparator
    {
        if (left < right)
        {
            int pivotIndex = Partition(comparer, left, right);
            Sort(comparer, left, pivotIndex - 1);
            Sort(comparer, pivotIndex + 1, right);
        }
    }

    private int Partition<TComparer>(TComparer comparer, int left, int right)
    where TComparer : IDatomComparator
    {
        var pivot = this[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (comparer.Compare(this[j], pivot) <= 0)
            {
                i++;
                Swap(i, j);
            }
        }

        Swap( i + 1, right);
        return i + 1;
    }

    /// <summary>
    /// Swaps the datoms at the given indices.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    private void Swap(int i, int j)
    {
        (_entityIds[i], _entityIds[j]) = (_entityIds[j], _entityIds[i]);
        (_attributeIds[i], _attributeIds[j]) = (_attributeIds[j], _attributeIds[i]);
        (_txIds[i], _txIds[j]) = (_txIds[j], _txIds[i]);
        (_flags[i], _flags[j]) = (_flags[j], _flags[i]);
        (_values[i], _values[j]) = (_values[j], _values[i]);
    }

    public void WriteTo<TBufferWriter>(TBufferWriter writer)
        where TBufferWriter : IBufferWriter<byte>
    {
        unsafe
        {
            var headerSpan = writer.GetSpan(sizeof(DataNodeHeader));
            ref var header = ref MemoryMarshal.AsRef<DataNodeHeader>(headerSpan);
            header._datomCount = (uint)_entityIds.Count;
            header._blobSize = (uint)_pooledMemoryBufferWriter.GetWrittenSpan().Length;
            header._version = (ushort)NodeVersions.DataNode;
            header._flags = 0x00;
            writer.Advance(sizeof(DataNodeHeader));

            var count = (int)header._datomCount;
            var span = writer.GetSpan(_entityIds.Count * sizeof(ulong));
            MemoryMarshal.Cast<ulong, byte>(CollectionsMarshal.AsSpan(_entityIds).SliceFast(0, count)).CopyTo(span);
            writer.Advance(_entityIds.Count * sizeof(ulong));

            span = writer.GetSpan(_attributeIds.Count * sizeof(ushort));
            MemoryMarshal.Cast<ushort, byte>(CollectionsMarshal.AsSpan(_attributeIds).SliceFast(0, count)).CopyTo(span);
            writer.Advance(_attributeIds.Count * sizeof(ushort));

            span = writer.GetSpan(_txIds.Count * sizeof(ulong));
            MemoryMarshal.Cast<ulong, byte>(CollectionsMarshal.AsSpan(_txIds).SliceFast(0, count)).CopyTo(span);
            writer.Advance(_txIds.Count * sizeof(ulong));

            span = writer.GetSpan(_flags.Count * sizeof(byte));
            MemoryMarshal.Cast<DatomFlags, byte>(CollectionsMarshal.AsSpan(_flags).SliceFast(0, count)).CopyTo(span);
            writer.Advance(_flags.Count * sizeof(byte));

            span = writer.GetSpan(_values.Count * sizeof(ulong));
            MemoryMarshal.Cast<ulong, byte>(CollectionsMarshal.AsSpan(_values).SliceFast(0, count)).CopyTo(span);
            writer.Advance(_values.Count * sizeof(ulong));

            var pooledWrittenSpan = _pooledMemoryBufferWriter.GetWrittenSpan();
            span = writer.GetSpan(pooledWrittenSpan.Length);
            pooledWrittenSpan.SliceFast(0, pooledWrittenSpan.Length).CopyTo(span);
            writer.Advance(pooledWrittenSpan.Length);
        }
    }

    /// <summary>
    /// Initializes the block from a span of bytes, the bytes can be written by the WriteTo method.
    /// </summary>
    /// <param name="span"></param>
    public void InitializeFrom(ReadOnlySpan<byte> span)
    {
        // Casts and copies the data from the span into the list
        ReadOnlySpan<byte> CopyToList<TValue>(List<TValue> list, ReadOnlySpan<byte> fromSpan, uint count)
            where TValue : struct
        {
            unsafe
            {
                var readSize = sizeof(TValue) * count;
                var listSpan = MemoryMarshal.Cast<byte, TValue>(fromSpan).SliceFast(0, (int)count);
                list.Clear();
                list.AddRange(listSpan);
                return fromSpan.SliceFast((int)readSize);
            }
        }

        unsafe
        {
            var header = MemoryMarshal.Read<DataNodeHeader>(span);
            var dataSection = span.SliceFast(sizeof(DataNodeHeader));

            dataSection =
                dataSection.CopyToLists(_entityIds, _attributeIds, _txIds,
                    _flags, _values, header._datomCount);

            var blobSpan = _pooledMemoryBufferWriter.GetSpan((int)header._blobSize);
            dataSection.SliceFast(0, (int)header._blobSize).CopyTo(blobSpan);
            _pooledMemoryBufferWriter.Advance((int)header._blobSize);
        }
    }

    public (int, bool) BinarySearch<TDatomIn, TDatomComparator>(scoped in TDatomIn datom, scoped in TDatomComparator comparator)
        where TDatomIn : IRawDatom
        where TDatomComparator : IDatomComparator
    {
        var lower = 0;
        var upper = Count - 1;

        while (lower <= upper)
        {
            var middle = lower + ((upper - lower) / 2);
            var middleDatom = new FlyweightRawDatom(this, (uint)middle);

            var comparison = comparator.Compare(datom, middleDatom);
            if (comparison == 0)
            {
                return (middle, true); // datom found
            }

            if (comparison < 0)
            {
                upper = middle - 1;
            }
            else
            {
                lower = middle + 1;
            }
        }

        return (lower, false); // datom not found, return the index where it would be inserted
    }

    public FlyweightIterator Seek<TDatomIn, TDatomComparator>(in TDatomIn datom, TDatomComparator comparator)
        where TDatomIn : IRawDatom
        where TDatomComparator : IDatomComparator
    {
        var (index, _) = BinarySearch(in datom, comparator);
        return new FlyweightIterator(this, index);
    }

    private class OuterComparator<TComparer>(OldAppendableNode node, TComparer comparer) : IComparer<int>
        where TComparer : IDatomComparator
    {
        public int Compare(int x, int y)
        {
            var a = new FlyweightRawDatom(node, (uint)x);
            var b = new FlyweightRawDatom(node, (uint)y);
            return comparer.Compare(a, b);
        }
    }



    public FlyweightRawDatom this[int index] => new(this, (uint)index);
    public INode Flush(NodeStore store)
    {
        return store.Flush(this);
    }

    IRawDatom INode.this[int index] => new FlyweightRawDatom(this, (uint)index);


    public struct FlyweightRawDatom(OldAppendableNode node, uint index) : IRawDatom
    {
        public ulong EntityId => node._entityIds[(int)index];
        public ushort AttributeId => node._attributeIds[(int)index];
        public ulong TxId => node._txIds[(int)index];
        public DatomFlags Flags => node._flags[(int)index];
        public ReadOnlySpan<byte> ValueSpan
        {
            get
            {
                if (((DatomFlags)Flags).HasFlag(DatomFlags.InlinedData))
                {
                    return ReadOnlySpan<byte>.Empty;
                }
                var combined = node._values[(int)index];
                var offset = (uint)(combined >> 4);
                var length = (uint)(combined & 0xF);
                return node._pooledMemoryBufferWriter.GetWrittenSpan().Slice((int)offset, (int)length);
            }
        }
        public ulong ValueLiteral => node._values[(int)index];

        public void Expand<TWriter>(out ulong entityId, out ushort attributeId, out ulong txId, out byte flags, in TWriter writer,
            out ulong valueLiteral) where TWriter : IBufferWriter<byte>
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return this.CommonToString();
        }
    }

    public struct FlyweightIterator(OldAppendableNode node, int idx)
        : IIterator<FlyweightRawDatom>
    {
        public bool Next()
        {
            if (idx >= node.Count - 1) return false;
            idx++;
            return true;
        }

        public bool AtEnd => idx >= node.Count;
        public bool Value(out FlyweightRawDatom value)
        {
            if (idx >= node.Count)
            {
                value = default;
                return false;
            }

            value = new FlyweightRawDatom(node, (uint)idx);
            return true;
        }

        /// <summary>
        /// The current index of the iterator, used for testing.
        /// </summary>
        public int Index => idx;
    }

    #region INode



    public INode Insert<TInput, TDatomComparator>(in TInput inputDatom, in TDatomComparator comparator)
    where TInput : IRawDatom
    where TDatomComparator : IDatomComparator
    {
        var (location, match) = BinarySearch(inputDatom, comparator);
        if (match)
        {
            return this;
        }

        _entityIds.Insert(location, inputDatom.EntityId);
        _attributeIds.Insert(location, inputDatom.AttributeId);
        _txIds.Insert(location, inputDatom.TxId);
        _flags.Insert(location, inputDatom.Flags);
        _values.Insert(location, inputDatom.ValueLiteral);
        if (((DatomFlags)inputDatom.Flags).HasFlag(DatomFlags.InlinedData))
            return this;

        var span = inputDatom.ValueSpan;
        var offset = _pooledMemoryBufferWriter.GetWrittenSpan().Length;
        _values.Insert(location, (ulong)((offset << 4) | span.Length));
        _pooledMemoryBufferWriter.Write(span);
        return this;
    }


    public INode Ingest<TIterator, TDatom, TDatomStop, TComparator>(in TIterator newData, in TDatomStop stopDatom, TComparator comparator)
        where TIterator : IIterator<TDatom>
        where TDatom : IRawDatom
        where TDatomStop : IRawDatom
        where TComparator : IDatomComparator
    {
        var newBlock = new OldAppendableNode(config);
        var current = Iterate();

        TDatom newDataVal;
        FlyweightRawDatom currentVal;

        var currentValid = current.Value(out currentVal);
        var newDataValid = newData.Value(out newDataVal);

        while (currentValid && newDataValid)
        {
            if (comparator.Compare(newDataVal, stopDatom) > 0)
            {
                break;
            }

            if (comparator.Compare(currentVal, newDataVal) == 0)
            {
                newBlock.Append(currentVal);
                currentValid = current.Next() && current.Value(out currentVal);
                newDataValid = newData.Next() && newData.Value(out newDataVal);
            }
            else if (comparator.Compare(currentVal, newDataVal) < 0)
            {
                newBlock.Append(currentVal);
                currentValid = current.Next() && current.Value(out currentVal);
            }
            else
            {
                newBlock.Append(newDataVal);
                newDataValid = newData.Next() && newData.Value(out newDataVal);
            }
        }

        while (currentValid)
        {
            newBlock.Append(currentVal);
            currentValid = current.Next() && current.Value(out currentVal);
        }

        while (newDataValid)
        {
            newBlock.Append(newDataVal);
            newDataValid = newData.Next() && newData.Value(out newDataVal);
            if (comparator.Compare(newDataVal, stopDatom) > 0)
            {
                break;
            }
        }

        return newBlock;
    }

    #endregion

    public FlyweightIterator Iterate()
    {
        return new FlyweightIterator(this, 0);
    }
}
