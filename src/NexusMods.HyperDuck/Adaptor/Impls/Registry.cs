using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class Registry : IRegistry
{
    private readonly IResultAdaptorFactory[] _resultAdaptorFactories;
    private readonly IRowAdaptorFactory[] _rowAdaptorFactories;
    private readonly IValueAdaptorFactory[] _valueAdaptorFactories;
    private readonly IBindingConverter[] _bindingConverters;
    private readonly Dictionary<Type, IBindingConverter> _bindingCache = new();
    private readonly AmbientSqlFragment[] _fragments;

    public Registry(IEnumerable<IResultAdaptorFactory> resultAdaptors, IEnumerable<IRowAdaptorFactory> rowAdaptors, IEnumerable<IValueAdaptorFactory> valueAdaptors, IEnumerable<IBindingConverter> bindingConverters, IEnumerable<AmbientSqlFragment> sqlFragments)
    {
        _resultAdaptorFactories = resultAdaptors.ToArray();
        _rowAdaptorFactories = rowAdaptors.ToArray();
        _valueAdaptorFactories = valueAdaptors.ToArray();
        _bindingConverters = bindingConverters.ToArray();
        _fragments = sqlFragments.ToArray();
    }
    
    public AmbientSqlFragment[] Fragments => _fragments;
    
    public IResultAdaptor<T> GetAdaptor<T>(Result result)
    {
        int bestPriority = Int32.MinValue;
        IResultAdaptorFactory bestFactory = null!;
        Type rowType = null!;
        
        Span<Result.ColumnInfo> columns = stackalloc Result.ColumnInfo[(int)result.ColumnCount];
        for (var idx = 0; idx < columns.Length; idx++)
        {
            columns[idx] = result.GetColumnInfo((ulong)idx);
        }
        
        foreach (var factory in _resultAdaptorFactories)
        {
            if (factory.TryExtractRowType(columns, typeof(T), out var thisRowType, out var priority))
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
        
        var rowFactory = CreateRowAdaptor(columns, rowType);
        
        var resultAdaptorType = bestFactory.CreateType(typeof(T), rowType!, rowFactory);
        
        return (IResultAdaptor<T>) Activator.CreateInstance(resultAdaptorType)!;
    }

    public IBindingConverter GetBindingConverter<T>(T obj)
    {
        var forType = obj!.GetType();
        lock (_bindingCache)
        {
            if (_bindingCache.TryGetValue(forType, out var found))
                return found;
        }
        
        int maxPriority = int.MinValue;
        IBindingConverter converter = null!;
        foreach (var bindingConverter in _bindingConverters)
        {
            if (bindingConverter.CanConvert(forType, out var priority))
            {
                if (priority > maxPriority)
                {
                    maxPriority = priority;
                    converter = bindingConverter;
                }
            }
        }
        if (maxPriority == int.MinValue)
            throw new InvalidOperationException("No binding converter found for {" + forType.FullName + "}");

        lock (_bindingCache)
        {
            _bindingCache.TryAdd(forType, converter);
            return converter;
        }
    }

    private Type CreateRowAdaptor(ReadOnlySpan<Result.ColumnInfo> columns, Type rowType)
    {
        int bestPriority = Int32.MinValue;
        IRowAdaptorFactory bestFactory = null!;
        Type[] innerTypes = [];

        foreach (var factory in _rowAdaptorFactories)
        {
            if (factory.TryExtractElementTypes(columns, rowType, out var thisInnerTypes, out var priority))
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
            using var logicalType = columns[idx].GetLogicalType();
            innerAdaptors.Add(CreateValueAdaptor(logicalType, idx, innerTypes[idx]));
        }
        
        return bestFactory.CreateType(rowType, innerTypes, innerAdaptors.ToArray());
    }

    public Type CreateValueAdaptor(LogicalType logicalType, int column, Type innerType)
    {
        int bestPriority = Int32.MinValue;
        IValueAdaptorFactory bestFactory = null!;
        Type[] innerTypes = [];
        
        foreach (var factory in _valueAdaptorFactories)
        {

            if (factory.TryExtractType(logicalType.TypeId, logicalType, innerType, out var thisInnerTypes, out var priority))
            {
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestFactory = factory;
                    innerTypes = thisInnerTypes;
                }
            }
        }
        
        if (bestFactory == null) throw new InvalidOperationException("No value adaptor found for {" + innerType.FullName + "} from DuckDB type {" + logicalType.TypeId + "}");

        var subAdaptors = new Type[innerTypes.Length];
        for (var idx = 0; idx < innerTypes.Length; idx++)
        {
            using var subType = logicalType.TypeId switch
            {
                DuckDbType.List => logicalType.ListChildType(),
                DuckDbType.Struct => logicalType.StructTypeChildType(idx),
                _ => throw new NotSupportedException($"Type {logicalType.TypeId} is not a supported container")
            };

            subAdaptors[idx] = CreateValueAdaptor(subType, idx, innerTypes[idx]);
        }

        return bestFactory.CreateType(this, logicalType.TypeId, logicalType, innerType, innerTypes, subAdaptors);
    }
}
