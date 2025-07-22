using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB;

/// <summary>
/// Container class for the DuckDB based query engine elements
/// </summary>
public class QueryEngine : IQueryEngine, IDisposable
{
    private readonly Registry _registry;
    private readonly Database _db;
    private readonly HyperDuck.Connection _conn;
    private readonly IAttribute[] _declaredAttributes;
    private readonly Dictionary<string, IAttribute> _attrsByShortName;
    private readonly LogicalType _attrEnumLogicalType;
    private readonly LogicalType _valueUnion;

    public QueryEngine(IServiceProvider services)
    {
        _registry = new Registry(services.GetServices<IResultAdaptorFactory>(),
            services.GetServices<IRowAdaptorFactory>(), services.GetServices<IValueAdaptorFactory>());
        _db = Database.OpenInMemory(_registry);
        _conn = _db.Connect();
        _declaredAttributes = services.GetServices<IAttribute>().OrderBy(a => a.Id.Id).ToArray();
        _attrsByShortName = _declaredAttributes.ToDictionary(a => a.ShortName, a => a);
        _attrEnumLogicalType = LogicalType.CreateEnum(_declaredAttributes.Select(s => s.ShortName).ToArray());
        _valueUnion = CreateValueType();
    }
    
    private LogicalType CreateValueType()
    {
        var names = new List<string>();
        var types = new List<LogicalType>();
        
        foreach (var value in Enum.GetNames<ValueTag>())
        {
            names.Add(value);
            types.Add(Enum.Parse<ValueTag>(value).DuckDbType());
        }
        return LogicalType.CreateUnion(names.ToArray(), CollectionsMarshal.AsSpan(types));
    }

    public LogicalType AttrEnum => _attrEnumLogicalType;

    public LogicalType ValueUnion => _valueUnion;

    public void Dispose()
    {
        _attrEnumLogicalType.Dispose();
        _db.Dispose();
        _conn.Dispose();
    }

    public HyperDuck.Connection Connection => _conn;
}
