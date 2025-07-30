using System;

namespace NexusMods.HyperDuck.Adaptor;

public interface IResultAdaptorFactory
{
    /// <summary>
    /// Given a result type, extract a row type (if possible) and return the priority for this handler
    /// </summary>
    public bool TryExtractRowType(ReadOnlySpan<Result.ColumnInfo> columns, Type resultType, out Type rowType, out int priority);
    
    /// <summary>
    /// Given a row type, create a new IResultAdaptor that uses the given row type and row adaptor type. The
    /// rowAdaptorType will inherit from IRowAdaptor 
    /// </summary>
    public Type CreateType(Type resultType, Type rowType, Type rowAdaptorType);
}
