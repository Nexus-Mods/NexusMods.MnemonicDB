namespace NexusMods.Query.Abstractions.Engines.Slots;

public class ValueSlot<T> : ISlot<T> where T : struct
{
    private readonly int _offset;

    public ValueSlot(int offset)
    {
        _offset = offset;
    }
    
    public T Get(ref Environment environment)
    {
        return environment.GetValue<T>(_offset);
    }

    public void Set(ref Environment environment, T value)
    {
        environment.SetValue(_offset, value);
    }
}
