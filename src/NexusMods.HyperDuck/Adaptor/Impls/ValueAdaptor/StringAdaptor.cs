using System;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class StringAdaptor<T> : IValueAdaptor<T>
{
    public static void Adapt(ValueCursor cursor, ref T? value)
    {
        var str = cursor.GetValue<StringElement>();
        value = (T)(object)str.GetString();
    }
}

public class StringAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        priority = 0;
        subTypes = [];
        return taggedType == DuckDbType.Varchar && type.IsAssignableFrom(typeof(string));
    }

    public Type CreateType(DuckDbType taggedType, LogicalType logicalType, Type resultTypes, Type[] subTypes)
    {
        return typeof(StringAdaptor<>).MakeGenericType(resultTypes);
    }
}
