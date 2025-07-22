using System;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class UnmanagedValueAdaptor<T> : IValueAdaptor<T>
    where T : unmanaged
{
    public static void Adapt(ValueCursor cursor, ref T value)
    {
        value = cursor.GetValue<T>();
    }
}


public class UnmanagedValueAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        subTypes = [];
        priority = 0;
        if (taggedType == DuckDbType.Integer || taggedType == DuckDbType.BigInt)
            return true;
        return false;
    }

    public Type CreateType(DuckDbType taggedType, LogicalType logicalType, Type resultTypes, Type[] subTypes)
    {
        return typeof(UnmanagedValueAdaptor<>).MakeGenericType(resultTypes);
    }
}
