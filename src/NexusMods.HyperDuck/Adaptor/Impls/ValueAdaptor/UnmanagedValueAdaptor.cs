namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class UnmanagedValueAdaptor<T> : IValueAdaptor<T>
    where T : unmanaged
{
    public static void Adapt(ValueCursor cursor, ref T value)
    {
        value = cursor.GetValue<T>();
    }
}
