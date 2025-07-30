namespace NexusMods.HyperDuck.Adaptor;

public interface IRowAdaptor
{
    
}

public interface IRowAdaptor<T> : IRowAdaptor
{
    public static abstract void Adapt(RowCursor cursor, ref T? value);
}
