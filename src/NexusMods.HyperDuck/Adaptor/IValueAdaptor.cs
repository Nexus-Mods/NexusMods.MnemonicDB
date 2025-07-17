namespace NexusMods.HyperDuck.Adaptor;

public interface IValueAdaptor
{
    
}


public interface IValueAdaptor<T>
{
    static abstract void Adapt(RowCursor cursor, ref T? value);
}
