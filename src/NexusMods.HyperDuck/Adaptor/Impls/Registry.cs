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
        _fragments = TopologicallySort(sqlFragments);
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
            if (logicalType.TypeId is DuckDbType.List)
            {
                using var subType = logicalType.ListChildType();
                subAdaptors[idx] = CreateValueAdaptor(subType, idx, innerTypes[idx]);
            } else if (logicalType.TypeId is DuckDbType.Struct)
            {
                using var subType = logicalType.StructTypeChildType(idx);
                subAdaptors[idx] = CreateValueAdaptor(subType, idx, innerTypes[idx]);
            }
        }

        return bestFactory.CreateType(this, logicalType.TypeId, logicalType, innerType, innerTypes, subAdaptors);
    }

    private static AmbientSqlFragment[] TopologicallySort(IEnumerable<AmbientSqlFragment> fragments)
    {
        var list = fragments.ToList();
        // Map namespace -> indices of fragments with that namespace
        var byNamespace = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (var i = 0; i < list.Count; i++)
        {
            var ns = list[i].Namespace ?? string.Empty;
            if (!byNamespace.TryGetValue(ns, out var bucket))
            {
                bucket = [];
                byNamespace[ns] = bucket;
            }
            bucket.Add(i);
        }

        // Build graph: edge from dependency -> dependent
        var edges = new List<int>[list.Count];
        var inDegree = new int[list.Count];
        for (var i = 0; i < list.Count; i++) edges[i] = [];

        for (var i = 0; i < list.Count; i++)
        {
            var reqs = list[i].Requires;
            foreach (var req in reqs)
            {
                if (byNamespace.TryGetValue(req, out var deps))
                {
                    foreach (var depIdx in deps)
                    {
                        edges[depIdx].Add(i);
                        inDegree[i]++;
                    }
                }
                // If the required namespace is not present, ignore; nothing to order against
            }
        }

        // Kahn's algorithm
        var queue = new Queue<int>();
        for (int i = 0; i < list.Count; i++) if (inDegree[i] == 0) queue.Enqueue(i);

        var result = new List<AmbientSqlFragment>(list.Count);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            result.Add(list[n]);
            foreach (var m in edges[n])
            {
                inDegree[m]--;
                if (inDegree[m] == 0) queue.Enqueue(m);
            }
        }

        if (result.Count != list.Count)
        {
            // Cycle detected; fall back to input order to avoid deadlock
            return list.ToArray();
        }

        return result.ToArray();
    }
}
