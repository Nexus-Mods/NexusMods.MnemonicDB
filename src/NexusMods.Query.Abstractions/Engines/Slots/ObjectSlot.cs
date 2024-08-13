namespace NexusMods.Query.Abstractions.Engines.Slots;

public struct ObjectSlot<T>(int idx) : ISlot<T> where T : class
{
    public T Get(ref Environment environment)
    {
        return environment.GetObject<T>(idx);
    }

    public void Set(ref Environment environment, T value)
    {
        environment.SetObject(idx, value);
    }
}
