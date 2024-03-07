using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Columns;
using NexusMods.EventSourcing.Storage.Columns.PackedColumns;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// A node that is appendable and not yet frozen. Can be sorted
/// after insertion.
/// </summary>
public class AppendableNode : ADataNode, IAppendableNode
{
    private readonly UnsignedIntegerColumn<EntityId> _entityIds;
    private readonly UnsignedIntegerColumn<AttributeId> _attributeIds;
    private UnsignedIntegerColumn<TxId> _transactionIds;
    private readonly UnsignedIntegerColumn<DatomFlags> _flags;
    private readonly AppendableBlobColumn _values;

    public override IColumn<EntityId> EntityIds => _entityIds;
    public override IColumn<AttributeId> AttributeIds => _attributeIds;
    public override IColumn<TxId> TransactionIds => _transactionIds;
    public override IColumn<DatomFlags> Flags => _flags;
    public override IBlobColumn Values => _values;

    // Empty constructor for serialization
    public AppendableNode()
    {
        _entityIds = new UnsignedIntegerColumn<EntityId>();
        _attributeIds = new UnsignedIntegerColumn<AttributeId>();
        _transactionIds = new UnsignedIntegerColumn<TxId>();
        _flags = new UnsignedIntegerColumn<DatomFlags>();
        _values = new AppendableBlobColumn();
    }

    public void Append(in Datom datom)
    {
        _entityIds.Append(datom.E);
        _attributeIds.Append(datom.A);
        _transactionIds.Append(datom.T);
        _flags.Append(datom.F);
        _values.Append(datom.V.Span);
    }

    /// <summary>
    /// Merges two pre-sorted nodes into a new node, using the given comparator.
    /// </summary>
    public static AppendableNode Merge(IDataNode nodeA, IDataNode nodeB, IDatomComparator comparator)
    {
        var newNode = new AppendableNode();
        foreach (var datom in nodeA.Merge(nodeB, comparator))
        {
            newNode.Append(datom);
        }
        return newNode;
    }

    public void Append<TValue>(EntityId e, AttributeId a, TxId t, DatomFlags f, IValueSerializer<TValue> serializer, TValue value)
    {
        _entityIds.Append(e);
        _attributeIds.Append(a);
        _transactionIds.Append(t);
        _flags.Append(f);
        _values.Append(serializer, value);
    }


    public override int Length => _entityIds.Length;

    /// <summary>
    /// Sorts the node using the given comparator.
    /// </summary>
    /// <param name="comparator"></param>
    public void Sort<TComparator>(TComparator comparator)
        where TComparator : IDatomComparator
    {
        // There's probably more ways we can optimize this, but it's good for now.
        // Essentially we're creating an array of indices, and then sorting that array
        // using a custom comparer that uses the indices to access the actual data.
        // Once we're done, we shuffle the data in the columns using the sorted indices.
        // This may not make sense at first, but the sorting algorithms may move the values around
        // in many different ways, so that A may move to B and D may move to C and C may move to A.
        // Meaning that we can't just swap the values because future swaps will be based on the original
        // positions of the values, not the new ones.
        //
        // Essentially we have to create keys for each entry, then create a new node based on the
        // sorted keys

        var pidxs = GC.AllocateUninitializedArray<int>(_entityIds.Length);

        unsafe
        {
            fixed (EntityId* entityIdsPtr = _entityIds.Data)
            fixed (AttributeId* attributeIdsPtr = _attributeIds.Data)
            fixed (TxId* transactionIdsPtr = _transactionIds.Data)
            fixed (DatomFlags* flagsPtr = _flags.Data)
            {

                for (var i = 0; i < pidxs.Length; i++)
                {
                    pidxs[i] = i;
                }

                var datoms = new MemoryDatom<AppendableBlobColumn>
                {
                    EntityIds = entityIdsPtr,
                    AttributeIds = attributeIdsPtr,
                    TransactionIds = transactionIdsPtr,
                    Flags = flagsPtr,
                    Values = _values
                };

                var comp = comparator.MakeComparer(datoms);

                Array.Sort(pidxs, 0, _entityIds.Length, comp);

                _entityIds.Shuffle(pidxs);
                _attributeIds.Shuffle(pidxs);
                _transactionIds.Shuffle(pidxs);
                _flags.Shuffle(pidxs);
                _values.Shuffle(pidxs);
            }
        }
    }

    /// <summary>
    /// Sorts the node using the given comparator.
    /// </summary>
    /// <param name="comparator"></param>
    public IDataNode AsSorted<TComparator>(TComparator comparator)
        where TComparator : IDatomComparator
    {
        // There's probably more ways we can optimize this, but it's good for now.
        // Essentially we're creating an array of indices, and then sorting that array
        // using a custom comparer that uses the indices to access the actual data.
        // Once we're done, we shuffle the data in the columns using the sorted indices.
        // This may not make sense at first, but the sorting algorithms may move the values around
        // in many different ways, so that A may move to B and D may move to C and C may move to A.
        // Meaning that we can't just swap the values because future swaps will be based on the original
        // positions of the values, not the new ones.
        //
        // Essentially we have to create keys for each entry, then create a new node based on the
        // sorted keys

        var pidxs = GC.AllocateUninitializedArray<int>(_entityIds.Length);

        unsafe
        {
            fixed (EntityId* entityIdsPtr = _entityIds.Data)
            fixed (AttributeId* attributeIdsPtr = _attributeIds.Data)
            fixed (TxId* transactionIdsPtr = _transactionIds.Data)
            fixed (DatomFlags* flagsPtr = _flags.Data)
            {

                for (var i = 0; i < pidxs.Length; i++)
                {
                    pidxs[i] = i;
                }

                var datoms = new MemoryDatom<AppendableBlobColumn>
                {
                    EntityIds = entityIdsPtr,
                    AttributeIds = attributeIdsPtr,
                    TransactionIds = transactionIdsPtr,
                    Flags = flagsPtr,
                    Values = _values
                };

                var comp = comparator.MakeComparer(datoms);

                Array.Sort(pidxs, 0, _entityIds.Length, comp);

                return new SortedNode(pidxs, this);
            }
        }
    }

    public override Datom this[int idx] => new() {
        E = _entityIds[idx],
        A = _attributeIds[idx],
        T = _transactionIds[idx],
        F = _flags[idx],
        V = _values[idx]
    };

    public override Datom LastDatom => this[Length - 1];


    public override void WriteTo<TWriter>(TWriter writer)
    {
        throw new NotSupportedException("Must pack the node before writing it to a buffer.");
    }

    public override IDataNode Flush(INodeStore store)
    {
        var packed = Pack();
        return store.Flush(packed);
    }

    public IDataNode Pack()
    {
        return new PackedNode(Length,
            _entityIds.Pack(),
            _attributeIds.Pack(),
            _transactionIds.Pack(),
            _flags.Pack(),
            _values.Pack());
    }

    public override IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
        {
            yield return this[i];
        }
    }

    /// <summary>
    /// Initializes a new node with the given datoms.
    /// </summary>
    public static AppendableNode Initialize(IEnumerable<Datom> datoms)
    {
        var node = new AppendableNode();
        foreach (var datom in datoms)
        {
            node.Append(datom);
        }
        return node;
    }

    public (AppendableNode A, AppendableNode B) Split()
    {
        var mid = Length / 2;
        var a = new AppendableNode();
        var b = new AppendableNode();

        for (var i = 0; i < mid; i++)
        {
            a.Append(this[i]);
        }

        for (var i = mid; i < Length; i++)
        {
            b.Append(this[i]);
        }
        return (a, b);
    }

    public void Append(IDataNode node)
    {
        // TODO: Vectorize this
        foreach (var t in node)
        {
            Append(t);
        }
    }

    public void SetTx(TxId nextTx)
    {
        _transactionIds = UnsignedIntegerColumn<TxId>.UnpackFrom(new ConstantPackedColumn<TxId, ulong>(_transactionIds.Length, nextTx));
    }

    public void RemapEntities(Func<EntityId, EntityId> remapper, AttributeRegistry registry)
    {
        unsafe
        {
            fixed (EntityId* entityIdsPtr = _entityIds.Data)
            {
                for (var i = 0; i < _entityIds.Length; i++)
                {
                    var attrId = _attributeIds[i];
                    if (registry.IsReference(_attributeIds[i]))
                    {
                        _values.RemapEntities(i, remapper);
                    }
                    entityIdsPtr[i] = remapper(entityIdsPtr[i]);
                }
            }
        }
    }
}
