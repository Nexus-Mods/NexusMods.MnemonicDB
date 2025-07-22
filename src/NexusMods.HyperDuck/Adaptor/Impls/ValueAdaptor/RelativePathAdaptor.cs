using System;
using NexusMods.Paths;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class RelativePathAdaptor : IValueAdaptor<RelativePath>
{
    public static void Adapt(ValueCursor cursor, ref RelativePath value)
    {
        var str = cursor.GetValue<StringElement>().GetString();
        value = RelativePath.FromUnsanitizedInput(str);
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

    public Type CreateType(DuckDbType taggedType, LogicalType logicalType, Type resultTypes, Type[] subTypes)
    {
        return typeof(RelativePathAdaptor);
    }
}
