using System;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class DateTimeOffsetAdaptor : IValueAdaptor<DateTimeOffset>
{
    public static void Adapt<TCursor>(TCursor cursor, ref DateTimeOffset value) where TCursor : IValueCursor, allows ref struct
    {
        var longValue = cursor.GetValue<long>();
        value = new DateTimeOffset(longValue, TimeSpan.Zero);
    }
}

public class DateTimeOffsetAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        if (taggedType == DuckDbType.BigInt && type == typeof(DateTimeOffset))
        {
            priority = 1;
            subTypes = [];
            return true;
        }
        
        priority = 0;
        subTypes = [];
        return false;
    }

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultTypes,
        Type[] subTypes, Type[] subAdaptors)
    {
        return typeof(DateTimeOffsetAdaptor);
    }
}
