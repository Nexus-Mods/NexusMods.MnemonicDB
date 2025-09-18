using System;
using NexusMods.Paths;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class RelativePathAdaptor : IValueAdaptor<RelativePath>
{
    public static bool Adapt<TCursor>(TCursor cursor, ref RelativePath value) 
        where TCursor : IValueCursor, allows ref struct
    {
        var str = cursor.GetValue<StringElement>().GetString();
        return Helpers.AssignNotEq(ref value, RelativePath.FromUnsanitizedInput(str));
    }
}

public class RelativePathAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        subTypes = [];
        priority = 0;
        if (taggedType == DuckDbType.Varchar && type == typeof(RelativePath))
            return true;
        return false;
    }

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultTypes,
        Type[] subTypes, Type[] subAdaptors)
    {
        return typeof(RelativePathAdaptor);
    }
}
