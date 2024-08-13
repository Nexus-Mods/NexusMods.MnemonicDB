namespace NexusMods.Query.Abstractions.Engines.Slots;

public interface ISlot<T>
{ 
    public T Get(ref Environment environment);
    public void Set(ref Environment environment, T value);
}
