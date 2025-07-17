namespace NexusMods.HyperDuck.Adaptor;

public interface IResultAdaptor
{
    
}

public interface IResultAdaptor<T>
{
    void Adapt(Result result, ref T value);
}
