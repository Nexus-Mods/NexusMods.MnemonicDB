namespace NexusMods.HyperDuck.Adaptor;

public interface IValueAdaptor
{
    
}


public interface IValueAdaptor<T>
{
    static abstract void Adapt<TCursor>(TCursor cursor, ref T? value) where TCursor : IValueCursor, allows ref struct;
}
