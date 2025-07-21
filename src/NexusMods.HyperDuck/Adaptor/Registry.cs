using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.HyperDuck.Adaptor;

public class Registry
{
    private readonly IResultAdaptorFactory[] _resultAdaptors;
    private readonly IRowAdaptorFactory[] _rowAdaptors;
    private readonly IValueAdaptorFactory[] _valueAdaptors;

    public Registry(IServiceProvider serviceProvider)
    {
        _resultAdaptors = serviceProvider.GetServices<IResultAdaptorFactory>().ToArray();
        _rowAdaptors = serviceProvider.GetServices<IRowAdaptorFactory>().ToArray();
        _valueAdaptors = serviceProvider.GetServices<IValueAdaptorFactory>().ToArray();
    }

    public IResultAdaptor CreateResultAdaptor(Result result, Type resultType)
    {
        var priority = Int32.MinValue;
        IResultAdaptorFactory? foundFactory = null;
        Type? rowType = null;
        
        foreach (var factory in _resultAdaptors)
        {
            if (!factory.TryExtractRowType(resultType, out rowType, out var factoryPriority)) 
                continue;
            
            if (factoryPriority > priority)
            {
                foundFactory = factory;
                rowType = rowType;
                priority = factoryPriority;
            }
        }
        
        if (foundFactory is null)
            throw new InvalidOperationException("No result adaptor found for {" + resultType.FullName + "}");
        
        var rowFactory = CreateRowAdaptor(result, rowType);
        
        var resultAdaptorType = foundFactory.CreateType(resultType, rowType!, rowFactory);
        
        return (IResultAdaptor) Activator.CreateInstance(resultAdaptorType)!;
    }

    private Type CreateRowAdaptor(Result result, Type? rowType)
    {
        throw new NotImplementedException();
    }
}

