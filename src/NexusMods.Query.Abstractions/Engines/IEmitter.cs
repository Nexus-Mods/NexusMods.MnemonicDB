namespace NexusMods.Query.Abstractions.Engines;

public interface IEmitter<T>
{
    public void Emit(T fact);
}
