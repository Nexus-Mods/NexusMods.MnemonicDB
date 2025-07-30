using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
    private readonly IAttribute[] _declaredAttributes;
    private readonly Dictionary<string, IAttribute> _attrsByShortName;
    private readonly LogicalType _attrEnumLogicalType;
    private readonly LogicalType _valueUnion;
    private readonly LogicalType _valueTagEnum;

    public QueryEngine(IServiceProvider services)
    {
        _registry = new Registry(services.GetServices<IResultAdaptorFactory>(),
            services.GetServices<IRowAdaptorFactory>(), services.GetServices<IValueAdaptorFactory>());
        _db = Database.OpenInMemory(_registry);
        _declaredAttributes = services.GetServices<IAttribute>().OrderBy(a => a.Id.Id).ToArray();
        _attrsByShortName = _declaredAttributes.ToDictionary(a => a.ShortName, a => a);
        _attrEnumLogicalType = LogicalType.CreateEnum(_declaredAttributes.Select(s => s.ShortName).ToArray());
        _valueTagEnum = MakeValueTagEnum();
        _valueUnion = CreateValueType();
    }

    private LogicalType MakeValueTagEnum()
    {
        var names = new string[(int)Enum.GetValues<ValueTag>().Max() + 1];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = Enum.GetName(typeof(ValueTag), i) ?? $"UnknownValueTag{i}";
        }
        return LogicalType.CreateEnum(names);
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

    public int AttrEnumWidth => _attrsByShortName.Count > 255 ? 2 : 1;
    

    public LogicalType ValueUnion => _valueUnion;

    public void Dispose()
    {
        /*
        _attrEnumLogicalType.Dispose();
        _db.Dispose();
        */
    }

    public HyperDuck.Database Database => _db;
    public LogicalType ValueTagEnum => _valueTagEnum;
}
