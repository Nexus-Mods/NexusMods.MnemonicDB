using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

[ValueObject<ulong>]
public readonly partial struct EntityId
{

}


public readonly struct EntityId<T> where T : notnull
{
    public T Value { get; }

    public EntityId(T value)
    {
        Value = value;
    }
}
