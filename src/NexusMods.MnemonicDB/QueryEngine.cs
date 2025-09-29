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
public class QueryEngine : IQueryEngine, IAsyncDisposable
{
    private readonly Registry _registry;
    private readonly HyperDuck.DuckDB _db;
    private readonly IAttribute[] _declaredAttributes;
    private readonly Dictionary<string, IAttribute> _attrsByShortName;
    private LogicalType _attrEnumLogicalType;
    private LogicalType _valueTagEnum;

    public QueryEngine(IServiceProvider services)
    {
        _registry = new Registry(services.GetServices<IResultAdaptorFactory>(),
            services.GetServices<IRowAdaptorFactory>(), 
            services.GetServices<IValueAdaptorFactory>(),
            services.GetServices<IBindingConverter>(),
            services.GetServices<AmbientSqlFragment>());
        _db = HyperDuck.DuckDB.Open(_registry, services.GetServices<ATableFunction>(), services.GetServices<AScalarFunction>());
        _declaredAttributes = services.GetServices<IAttribute>().OrderBy(a => a.Id.Id).ToArray();
        _attrsByShortName = _declaredAttributes.ToDictionary(a => a.ShortName, a => a);
        _attrEnumLogicalType = LogicalType.CreateEnum(_declaredAttributes.Select(s => s.ShortName).ToArray());
        _valueTagEnum = MakeValueTagEnum();
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

    public LogicalType AttrEnum => _attrEnumLogicalType;

    public int AttrEnumWidth => _attrsByShortName.Count > 255 ? 2 : 1;


    public async ValueTask DisposeAsync()
    {
        _valueTagEnum.Dispose();
        _attrEnumLogicalType.Dispose();
        await _db.DisposeAsync().ConfigureAwait(false);
    }

    public HyperDuck.DuckDB DuckDb => _db;
    public LogicalType ValueTagEnum => _valueTagEnum;
}
