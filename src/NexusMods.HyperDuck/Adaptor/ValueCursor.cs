using System;

namespace NexusMods.HyperDuck.Adaptor;

public ref struct ValueCursor : IValueCursor
{
    private readonly ReadOnlySpan<ReadOnlyVector> _vectors;

    public ValueCursor(RowCursor rowCursor)
    {
        _rowCursor = rowCursor;
    }
    
    public int ColumnIndex;
    private readonly RowCursor _rowCursor;

    public T GetValue<T>() where T : unmanaged
    {
        return _rowCursor.GetValue<T>(ColumnIndex);
    }

    public ReadOnlyVector GetListChild()
    {
        return _rowCursor.GetListSubVector(ColumnIndex);
    }

    public bool IsNull => _rowCursor.IsNull(ColumnIndex);
}
