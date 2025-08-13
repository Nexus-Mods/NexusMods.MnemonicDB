using System;
using NexusMods.HyperDuck.Adaptor.Impls;

namespace NexusMods.HyperDuck.Adaptor;

public interface IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority);

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultTypes,
        Type[] subTypes, Type[] subAdaptors);
}
