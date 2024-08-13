using System;

namespace NexusMods.Query.Abstractions.Engines.Slots;

public struct ConstantSlot<T> : ISlot<T>
{
    private readonly T _value;

    public ConstantSlot(T value)
    {
        _value = value;
    }
    
    public T Get(ref Environment environment)
    {
        return _value;
    }

    public void Set(ref Environment environment, T value)
    {
        throw new NotSupportedException("Cannot set a constant slot");
    }
}
