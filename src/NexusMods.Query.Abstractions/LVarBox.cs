namespace NexusMods.Query.Abstractions;

public class LVarBox<T> : ILVarBox
{
    public T Value = default(T)!;
}
