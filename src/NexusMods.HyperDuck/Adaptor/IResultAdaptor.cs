namespace NexusMods.HyperDuck.Adaptor;

public interface IResultAdaptor
{
    
}

public interface IResultAdaptor<T>
{
     bool Adapt(Result result, ref T value);
}
