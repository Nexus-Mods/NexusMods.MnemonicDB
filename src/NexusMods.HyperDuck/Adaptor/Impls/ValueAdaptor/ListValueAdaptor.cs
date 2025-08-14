using System;
using System.Collections.Generic;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class ListValueAdaptor<TList, TValue, TAdaptor> : IValueAdaptor<TList>
    where TList : IList<TValue>, new()
    where TAdaptor : IValueAdaptor<TValue>
{
    public static void Adapt<TCursor>(TCursor cursor, ref TList? value) 
        where TCursor : IValueCursor, allows ref struct
    {
        var element = cursor.GetValue<ListEntry>();
        var subVector = new SubVectorCursor(cursor.GetListChild());
        SetCapacity(ref value, (int)element.Length);
        
        for (ulong i = 0; i < element.Length; i++)
        {
            subVector.RowIndex = element.Offset + i;
            var itm = value![(int)i];
            TAdaptor.Adapt(subVector, ref itm);
            value[(int)i] = itm!;
        }
    }

    private static void SetCapacity(ref TList? value, int size)
    {
        value ??= [];

        if (size == 0)
        {
            value.Clear();
        }
        else if (value.Count > size)
        {
            while (value.Count > size)
            {
                value.RemoveAt(value.Count - 1);
            }
        }
        else if (value.Count < size)
        {
            while (value.Count < size)
            {
                value.Add(default!);
            }
        }
    }
}

public class ListValueAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        if (taggedType == DuckDbType.List &&
            type.TryExtractGenericInterfaceArguments(typeof(List<>), out var elementTypes))
        {
            priority = 0;
            subTypes = elementTypes;
            return true;
        }
        priority = 0;
        subTypes = [];
        return false;
    }

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultType,
        Type[] subTypes, Type[] subAdaptors)
    {
        return typeof(ListValueAdaptor<,,>).MakeGenericType(resultType, subTypes[0], subAdaptors[0]);
    }
}
