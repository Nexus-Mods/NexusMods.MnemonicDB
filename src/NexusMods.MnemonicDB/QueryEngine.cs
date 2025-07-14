using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB;

public class QueryEngine : IQueryEngine
{
    private readonly ILogger<QueryEngine> _logger;
    private Database _db;
    private HyperDuck.Connection _conn;
    private readonly Builder _builder;
    private readonly Registry _registry;

    public QueryEngine(ILogger<QueryEngine> logger, IEnumerable<IConverter> converters)
    {
        _logger = logger;

        _db = Database.OpenInMemory();
        _conn = _db.Connect();
        _registry = new Registry(converters);
        _builder = new Builder(_registry);
    }
    
    public void Register(ATableFunction tableFunction)
    {
        _conn.Register(tableFunction);
    }

    public T Query<T>(string sql)
    {
        return _conn.Query<T>(sql, _builder);
    }
}
