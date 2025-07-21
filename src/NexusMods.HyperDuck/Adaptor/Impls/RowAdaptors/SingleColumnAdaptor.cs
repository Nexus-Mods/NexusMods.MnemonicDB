using System;

namespace NexusMods.HyperDuck.Adaptor.Impls.RowAdaptors;

public struct SingleColumnAdaptor<TItem, TValueAdaptor> : IRowAdaptor<TItem>
    where TValueAdaptor : IValueAdaptor<TItem>, new()
{
    public static void Adapt(RowCursor cursor, ref TItem? value)
    {
        var valueCursor = new ValueCursor(cursor);
        TValueAdaptor.Adapt(valueCursor, ref value);
    }
}

public class SingleColumnAdaptorFactory : IRowAdaptorFactory
{
    public bool TryExtractElementTypes(Type resultType, out Type[] elementTypes, out int priority)
    {
        priority = -10;
        elementTypes = [resultType];
        return true;
    }

    public Type CreateType(Type resultType, Type[] elementTypes, Type[] elementAdaptors)
    {
        return typeof(SingleColumnAdaptor<,>).MakeGenericType(elementTypes[0], elementAdaptors[0]);
    }
}
