using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class Registry : IRegistry
{
    private readonly IResultAdaptorFactory[] _resultAdaptorFactories;

    public Registry(IEnumerable<IResultAdaptorFactory> resultAdaptors)
    {
        _resultAdaptorFactories = resultAdaptors.ToArray();
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
        
        throw new NotImplementedException();

    }
}
