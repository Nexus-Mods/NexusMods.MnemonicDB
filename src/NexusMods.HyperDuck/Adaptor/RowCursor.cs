using System;

namespace NexusMods.HyperDuck.Adaptor;

/// <summary>
/// A flyweight implementation of a row, internally this will reference the underlying vectors of a chunk
/// </summary>
public ref struct RowCursor
{
    public int RowIndex;
    private readonly ReadOnlySpan<ReadOnlyVector> _vectors;

    public RowCursor(ReadOnlySpan<ReadOnlyVector> vectors)
    {
        RowIndex = 0;
        _vectors = vectors;
    }

    public ReadOnlySpan<T> GetData<T>(int column) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public ReadOnlyVector GetListSubVector(int column)
    {
        return _vectors[column].GetListChild();
    }

    public ReadOnlyVector GetStructChild(int column, ulong fieldIndex) => _vectors[column].GetStructChild(idx: fieldIndex);

    public T GetValue<T>(int columnIndex) where T : unmanaged
    {
        return _vectors[columnIndex].GetData<T>()[RowIndex];
    }

    public bool IsNull(int columnIndex) => _vectors[columnIndex].IsNull((ulong)RowIndex);
}
