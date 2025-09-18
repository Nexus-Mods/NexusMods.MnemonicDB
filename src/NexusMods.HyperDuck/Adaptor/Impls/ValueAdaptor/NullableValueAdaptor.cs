using System;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class NullableValueAdaptor<TInnerAdaptor, TType> : IValueAdaptor<Nullable<TType>>
    where TInnerAdaptor : IValueAdaptor<TType>
    where TType : struct
{
    public static bool Adapt<TCursor>(TCursor cursor, ref TType? value) where TCursor : IValueCursor, allows ref struct
    {
        if (cursor.IsNull)
            return Helpers.AssignNotEq(ref value, null);
        else
        {
            TType innerValue = default!;
            var change = TInnerAdaptor.Adapt(cursor, ref innerValue);
            return Helpers.AssignNotEq(ref value, innerValue) || change; 
        }
    }
}

public class NullableValueAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        if (type.TryExtractGenericInterfaceArguments(typeof(Nullable<>), out var innerTypes))
        {
            priority = 10;
            subTypes = innerTypes;
            return true;
        }
        priority = 0;
        subTypes = [];
        return false;
    }

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultTypes,
        Type[] subTypes, Type[] subAdaptors)
    {
        var subAdaptor = registry.CreateValueAdaptor(logicalType, 0, subTypes[0]);
        return typeof(NullableValueAdaptor<,>).MakeGenericType(subAdaptor, subTypes[0]);
    }
}
