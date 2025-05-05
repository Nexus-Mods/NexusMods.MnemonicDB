namespace NexusMods.MnemonicDB.Abstractions;

public struct Maybe<T>
{
    private T _value;
    
    private bool _isEmpty;
    
    public bool HasValue => !_isEmpty;
    
    public bool IsEmpty => _isEmpty;
    
    public T Value => _value;
}
