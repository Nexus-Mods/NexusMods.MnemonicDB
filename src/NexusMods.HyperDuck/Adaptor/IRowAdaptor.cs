namespace NexusMods.HyperDuck.Adaptor;

public interface IRowAdaptor
{
    
}

public interface IRowAdaptor<T> : IRowAdaptor
{
    public static abstract bool Adapt(RowCursor cursor, ref T? value);
}
