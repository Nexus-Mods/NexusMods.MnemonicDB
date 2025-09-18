using System;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class StringAdaptor<T> : IValueAdaptor<T> where T : IEquatable<T>
{
    public static bool Adapt<TCursor>(TCursor cursor, ref T? value) 
        where TCursor : IValueCursor, allows ref struct
    {
        if (cursor.IsNull)
        {
            return Helpers.AssignNotEq(ref value, default(T)!);       
        }

        var str = cursor.GetValue<StringElement>();
        return Helpers.AssignNotEq(ref value, (T)(object)str.GetString());
    }
}

public class StringAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        priority = 0;
        subTypes = [];
        return taggedType == DuckDbType.Varchar && type.IsAssignableTo(typeof(string));
    }

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultTypes,
        Type[] subTypes, Type[] subAdaptors)
    {
        return typeof(StringAdaptor<>).MakeGenericType(resultTypes);
    }
}
