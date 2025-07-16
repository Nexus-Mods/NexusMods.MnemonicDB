using System;
using System.Collections.Generic;

namespace NexusMods.HyperDuck.Adaptor.Impls.ResultAdaptors;

public class ListAdaptor<TRowType, TRowAdaptor> : IResultAdaptor 
    where TRowAdaptor : IRowAdaptor<TRowType>
{
    
}

public class ListAdaptorFactory : IResultAdaptorFactory
{
    public bool TryExtractRowType(Type resultType, out Type rowType, out int priority)
    {
        if (resultType.TryExtractGenericInterfaceArguments(typeof(List<>), out var genericArguments))
        {
            rowType = genericArguments[0];
            priority = 1;
            return true;
        }
        rowType = default!;
        priority = 0;
        return false;
    }

    public Type CreateType(Type rowType, Type rowAdaptorType)
    {
        return typeof(ListAdaptor<,>).MakeGenericType(rowType, rowAdaptorType);
    }
}
