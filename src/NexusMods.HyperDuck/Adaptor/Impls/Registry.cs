using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class Registry : IRegistry
{
    private readonly IResultAdaptorFactory[] _resultAdaptorFactories;
    private readonly IRowAdaptorFactory[] _rowAdaptorFactories;
    private readonly IValueAdaptorFactory[] _valueAdaptorFactories;

    public Registry(IEnumerable<IResultAdaptorFactory> resultAdaptors, IEnumerable<IRowAdaptorFactory> rowAdaptors, IEnumerable<IValueAdaptorFactory> valueAdaptors)
    {
        _resultAdaptorFactories = resultAdaptors.ToArray();
        _rowAdaptorFactories = rowAdaptors.ToArray();
        _valueAdaptorFactories = valueAdaptors.ToArray();
    }
    
    public IResultAdaptor<T> GetAdaptor<T>(Result result)
    {
        int bestPriority = Int32.MinValue;
        IResultAdaptorFactory bestFactory = null!;
        Type rowType = null!;
        
        foreach (var factory in _resultAdaptorFactories)
        {
            if (factory.TryExtractRowType(typeof(T), out var thisRowType, out var priority))
            {
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestFactory = factory;
                    rowType = thisRowType;
                }
            }
        }
        
        if (bestFactory is null)
            throw new InvalidOperationException("No result adaptor found for {" + typeof(T).FullName + "}");
        
        var rowFactory = CreateRowAdaptor(result, rowType);
        
        var resultAdaptorType = bestFactory.CreateType(typeof(T), rowType!, rowFactory);
        
        return (IResultAdaptor<T>) Activator.CreateInstance(resultAdaptorType)!;
    }
    
    private Type CreateRowAdaptor(Result result, Type rowType)
    {
        int bestPriority = Int32.MinValue;
        IRowAdaptorFactory bestFactory = null!;
        Type[] innerTypes = [];

        foreach (var factory in _rowAdaptorFactories)
        {
            if (factory.TryExtractElementTypes(rowType, out var thisInnerTypes, out var priority))
            {
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestFactory = factory;
                    innerTypes = thisInnerTypes;
                }
            }
        }
        
        if (bestFactory == null)
            throw new InvalidOperationException("No row adaptor found for {" + rowType.FullName + "}");

        var innerAdaptors = new List<Type>(innerTypes.Length);
        for (int idx = 0; idx < innerTypes.Length; idx++)
        {
            innerAdaptors.Add(CreateValueAdaptor(result, idx, innerTypes[idx]));
        }
        
        return bestFactory.CreateType(rowType, innerTypes, innerAdaptors.ToArray());
    }

    private Type CreateValueAdaptor(Result result, int column, Type innerType)
    {
        int bestPriority = Int32.MinValue;
        IValueAdaptorFactory bestFactory = null!;
        Type[] innerTypes = [];
        var columnInfo = result.GetColumnInfo((ulong)column);
        using var ddType = columnInfo.GetLogicalType();
        
        foreach (var factory in _valueAdaptorFactories)
        {

            if (factory.TryExtractType(columnInfo.Type, ddType, innerType, out var thisInnerTypes, out var priority))
            {
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestFactory = factory;
                    innerTypes = thisInnerTypes;
                }
            }
        }
        
        if (bestFactory == null)
            throw new InvalidOperationException("No value adaptor found for {" + innerType.FullName + "}");
        
        return bestFactory.CreateType(columnInfo.Type, ddType, innerType, innerTypes);
    }
}
