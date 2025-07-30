using System;

namespace NexusMods.HyperDuck.Adaptor;

public interface IRowAdaptorFactory
{
    /// <summary>
    /// Given a result type, extract a row type (if possible) and return the priority for this handler
    /// </summary>
    public bool TryExtractElementTypes(ReadOnlySpan<Result.ColumnInfo> result, Type resultType, out Type[] elementTypes, out int priority);

    public Type CreateType(Type resultType, Type[] elementTypes, Type[] elementAdaptors);
}
