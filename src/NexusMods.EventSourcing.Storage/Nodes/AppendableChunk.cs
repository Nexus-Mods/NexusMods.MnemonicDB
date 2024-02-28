using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Algorithms;
using NexusMods.EventSourcing.Storage.Columns;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// A chunk that is appendable and not yet frozen. Can be sorted
/// after insertion.
/// </summary>
public class AppendableChunk : IDataChunk, IAppendableChunk
{
    private readonly UnsignedIntegerColumn<EntityId> _entityIds;
    private readonly UnsignedIntegerColumn<AttributeId> _attributeIds;
    private readonly UnsignedIntegerColumn<TxId> _transactionIds;
    private readonly UnsignedIntegerColumn<DatomFlags> _flags;
    private readonly AppendableBlobColumn _values;

    public IColumn<EntityId> EntityIds => _entityIds;
    public IColumn<AttributeId> AttributeIds => _attributeIds;
    public IColumn<TxId> TransactionIds => _transactionIds;
    public IColumn<DatomFlags> Flags => _flags;
    public IBlobColumn Values => _values;

    // Empty constructor for serialization
    public AppendableChunk()
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

    public void Append<TValue>(EntityId e, AttributeId a, TxId t, DatomFlags f, IValueSerializer<TValue> serializer, TValue value)
    {
        _entityIds.Append(e);
        _attributeIds.Append(a);
        _transactionIds.Append(t);
        _flags.Append(f);
        _values.Append(serializer, value);
    }


    public int Length => _entityIds.Length;

    /// <summary>
    /// Sorts the chunk using the given comparator.
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
        // Essentially we have to create keys for each entry, then create a new chunk based on the
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

    public Datom this[int idx] => new() {
        E = _entityIds[idx],
        A = _attributeIds[idx],
        T = _transactionIds[idx],
        F = _flags[idx],
        V = _values[idx]
    };

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotSupportedException("Must pack the chunk before writing it to a buffer.");
    }

    public IDataChunk Pack()
    {
        return new PackedChunk(Length,
            _entityIds.Pack(),
            _attributeIds.Pack(),
            _transactionIds.Pack(),
            _flags.Pack(),
            _values.Pack());
    }

}
