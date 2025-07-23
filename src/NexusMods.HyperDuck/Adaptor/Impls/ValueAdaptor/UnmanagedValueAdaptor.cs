using System;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class UnmanagedValueAdaptor<T> : IValueAdaptor<T>
    where T : unmanaged
{
    public static void Adapt<TCursor>(TCursor cursor, ref T value) 
        where TCursor : IValueCursor, allows ref struct
    {
        value = cursor.GetValue<T>();
    }
}

public class BoolAdaptor : IValueAdaptor<bool>
{
    public static void Adapt<TCursor>(TCursor cursor, ref bool value) 
        where TCursor : IValueCursor, allows ref struct
    {
        value = cursor.GetValue<byte>() != 0;
    }
}


public class UnmanagedValueAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        subTypes = [];
        priority = 0;
        if (!type.IsValueType)
            return false;
        switch (taggedType)
        {
            case DuckDbType.Boolean:
            case DuckDbType.TinyInt:
            case DuckDbType.SmallInt:    
            case DuckDbType.Integer:
            case DuckDbType.BigInt:
            case DuckDbType.UTinyInt:
            case DuckDbType.USmallInt:
            case DuckDbType.UInteger:
            case DuckDbType.UBigInt:
            case DuckDbType.Float:
            case DuckDbType.Double:
            case DuckDbType.Timestamp:
            case DuckDbType.Hugeint:
            case DuckDbType.Uhugeint:
                break;
            default:
                return false;
        }
        return true;
    }

    public Type CreateType(DuckDbType taggedType, LogicalType logicalType, Type resultTypes, Type[] subTypes, Type[] subAdaptors)
    {
        if (resultTypes == typeof(bool))
            return typeof(BoolAdaptor);
        return typeof(UnmanagedValueAdaptor<>).MakeGenericType(resultTypes);
    }
}
