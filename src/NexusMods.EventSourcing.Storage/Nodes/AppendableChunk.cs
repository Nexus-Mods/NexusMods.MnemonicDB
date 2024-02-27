using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;

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


    public int Length => _entityIds.Length;

    /// <summary>
    /// Sorts the chunk using the given comparator.
    /// </summary>
    /// <param name="comparator"></param>
    public void Sort<TComparator>(TComparator comparator)
        where TComparator : IDatomComparator
    {
        Sort(comparator, 0, _entityIds.Length - 1);
    }

    public Datom this[int idx] => new() {
        E = _entityIds[idx],
        A = _attributeIds[idx],
        T = _transactionIds[idx],
        F = _flags[idx],
        V = _values[idx]
    };

    #region SortImplementation
    private void Swap(int a, int b)
    {
        _entityIds.Swap(a, b);
        _attributeIds.Swap(a, b);
        _transactionIds.Swap(a, b);
        _flags.Swap(a, b);
        _values.Swap(a, b);
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
        var i = left - 1;

        for (var j = left; j < right; j++)
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

    #endregion

}
