using System;

namespace NexusMods.HyperDuck.Adaptor;

public interface IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority);

    public IValueAdaptor CreateType(DuckDbType taggedType, LogicalType logicalType, Type resultTypes, Type[] subTypes);
}
