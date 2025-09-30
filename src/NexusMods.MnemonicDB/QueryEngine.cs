using System;
using System.Collections.Concurrent;
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
using NexusMods.MnemonicDB.QueryFunctions;

namespace NexusMods.MnemonicDB;

/// <summary>
/// Container class for the DuckDB based query engine elements
/// </summary>
public class QueryEngine : IQueryEngine, IAsyncDisposable
{
    public record ActiveConnection(ushort LocalId, string Name, ushort UniqueId, IConnection Connection);
    
    private readonly Registry _registry;
    private readonly HyperDuck.DuckDB _db;
    private readonly IAttribute[] _declaredAttributes;
    private readonly Dictionary<string, IAttribute> _attrsByShortName;
    private LogicalType _attrEnumLogicalType;
    private LogicalType _valueTagEnum;
    private ConcurrentDictionary<ushort, ActiveConnection> _connections = new();
    private readonly ModelDefinition[] _models;
    private List<ATableFunction> _registeredTableFunctions = new();
    
    public record AttrEnumEntry(IAttribute Attribute, ushort EnumId, string ShortName);

    public readonly AttrEnumEntry[] AttrEnumEntries;

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
        _attrEnumLogicalType = LogicalType.CreateEnum(_declaredAttributes.Select(s => s.ShortName).Prepend("UNKNOWN").ToArray());
        
        var entries = new List<AttrEnumEntry>();
        entries.Add(new AttrEnumEntry(null!, 0, "UNKNOWN"));
        var idx = 1;
        foreach (var entry in _declaredAttributes)
        {
            entries.Add(new AttrEnumEntry(entry, (ushort)idx, entry.ShortName));
            idx += 1;
        }

        AttrEnumEntries = entries.ToArray();
        
        _models = services.GetServices<ModelDefinition>().ToArray();
        RegisterModels();
        DuckDb.Register(new DatomsTableFunction(this));
        DuckDb.Register(new ActiveConnectionsTable(this));
    }

    private void RegisterModels()
    { 
        foreach (var model in _models)
        {
            var tableFn = new ModelTableFunction(this, model);
            DuckDb.Register(tableFn);
            _registeredTableFunctions.Add(tableFn);
        }
    }

    public List<ATableFunction> RegisteredTableFunctions => _registeredTableFunctions;
    public Dictionary<string, IAttribute> AttributesByShortName => _attrsByShortName;
    
    public ActiveConnection[] Connections => _connections.Values.ToArray();

    internal ushort RegisterConnection(IConnection connection, string name)
    {
        var uid = DuckDb.RegisterGlobalObject(connection);
        _connections[uid] = new ActiveConnection(uid, name, uid, connection);
        return uid;
    }

    internal void RemoveConnection(ushort uid)
    {
        if (_connections.Remove(uid, out _)) 
            DuckDb.DisposeGlobalObject(uid);
    }

    internal IConnection? GetConnectionByName(string name = "default")
    {
        foreach (var (_, item) in _connections)
            if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                return item.Connection;
        return null;
    }

    internal IConnection? GetConnectionByUid(ushort uid)
    {
        return _connections.TryGetValue(uid, out var item) ? item.Connection : null;
    }



    public LogicalType AttrEnum => _attrEnumLogicalType;

    public int AttrEnumWidth => _attrsByShortName.Count > 255 ? 2 : 1;


    public async ValueTask DisposeAsync()
    {
        _attrEnumLogicalType.Dispose();
        await _db.DisposeAsync().ConfigureAwait(false);
    }

    public HyperDuck.DuckDB DuckDb => _db;
}
