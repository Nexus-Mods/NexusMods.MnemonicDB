using System;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB;

/// <summary>
/// Container class for the DuckDB based query engine elements
/// </summary>
public class QueryEngine : IQueryEngine, IDisposable
{
    private readonly Registry _registry;
    private readonly Database _db;
    private readonly HyperDuck.Connection _conn;

    public QueryEngine(IServiceProvider services)
    {
        _registry = new Registry(services.GetServices<IResultAdaptorFactory>(),
            services.GetServices<IRowAdaptorFactory>(), services.GetServices<IValueAdaptorFactory>());
        _db = Database.OpenInMemory(_registry);
        _conn = _db.Connect();

    }
    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    public HyperDuck.Connection Connection => _conn;
}
