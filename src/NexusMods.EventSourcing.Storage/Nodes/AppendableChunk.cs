using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Algorithms;
using NexusMods.EventSourcing.Storage.Columns;
using NexusMods.EventSourcing.Storage.Datoms;

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
        var pidxs = GC.AllocateUninitializedArray<int>(_entityIds.Length);

        for (var i = 0; i < _entityIds.Length; i++)
        {
            pidxs[i] = i;
        }

        unsafe
        {
            fixed (EntityId* entityIdsPtr = _entityIds.Data)
            fixed (AttributeId* attributeIdsPtr = _attributeIds.Data)
            fixed (TxId* transactionIdsPtr = _transactionIds.Data)
            fixed (DatomFlags* flagsPtr = _flags.Data)
            fixed (int* pidxsPtr = pidxs)
            {
                var datoms = new MemoryDatom<AppendableBlobColumn>
                {
                    EntityIds = entityIdsPtr,
                    AttributeIds = attributeIdsPtr,
                    TransactionIds = transactionIdsPtr,
                    Flags = flagsPtr,
                    Values = _values
                };
                Sort(comparator, ref datoms, pidxsPtr, 0, _entityIds.Length - 1);
            }
        }

        for (var i = 0; i < _entityIds.Length; i++)
        {
            var idx = pidxs[i];
            if (idx != i)
            {
                _entityIds.Swap(i, idx);
                _attributeIds.Swap(i, idx);
                _transactionIds.Swap(i, idx);
                _flags.Swap(i, idx);
                _values.Swap(i, idx);
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

    #region SortImplementation
    private unsafe void Swap(int* pidxs, int a, int b)
    {
        (pidxs[a], pidxs[b]) = (pidxs[b], pidxs[a]);
    }

    private unsafe void Sort<TComparer>(TComparer comparer, ref MemoryDatom<AppendableBlobColumn> datoms, int* pidxs, int left, int right)
        where TComparer : IDatomComparator

    {
        if (left < right)
        {
            int pivotIndex = Partition(comparer, ref datoms, pidxs, left, right);
            Sort(comparer, ref datoms, pidxs, left, pivotIndex - 1);
            Sort(comparer, ref datoms, pidxs, pivotIndex + 1, right);
        }
    }

    private unsafe int Partition<TComparer>(TComparer comparer, ref MemoryDatom<AppendableBlobColumn> datoms, int* pidxs, int left, int right)
        where TComparer : IDatomComparator
    {
        var pivot = right;
        var i = left - 1;

        for (var j = left; j < right; j++)
        {
            if (comparer.Compare(datoms, pidxs[j], pidxs[pivot]) <= 0)
            {
                i++;
                Swap(pidxs, i, j);
            }
        }

        Swap(pidxs,  i + 1, right);
        return i + 1;
    }

    #endregion

}
