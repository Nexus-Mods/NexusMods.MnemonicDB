namespace NexusMods.HyperDuck.Adaptor;

public ref struct SubVectorCursor : IValueCursor
{
    private readonly ReadOnlyVector _vector;
    
    public SubVectorCursor(ReadOnlyVector vector)
    {
        _vector = vector;
    }

    public ulong RowIndex { get; set; }

    public T GetValue<T>() where T : unmanaged
    {
        return _vector.GetData<T>()[(int)RowIndex];
    }

    public ReadOnlyVector GetListChild()
    {
        throw new System.NotImplementedException();
    }

    public ReadOnlyVector GetStructChild(ulong fieldIndex) => _vector.GetStructChild(idx: fieldIndex);

    public bool IsNull => _vector.IsNull(RowIndex);
}
