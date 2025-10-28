using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB;

public class ConnectionFactory : IConnectionFactory
{
    /// <summary>
    /// Creates a connection from the given service provider, pulling the datom store settings from the service provider.
    /// </summary>
    public IConnection Create(IServiceProvider services)
    {
        var settings = services.GetRequiredService<DatomStoreSettings>();
        return Create(services, settings);
    }

    /// <summary>
    /// Creates a standard connection based on the given settings, and setting up the storage, query engine, and attribute resolver.
    /// </summary>
    public IConnection Create(IServiceProvider services, DatomStoreSettings settings)
    {
        // Loads Attributes from the service provider, and passes the provider to the serializers
        var resolver = new AttributeResolver(services, new AttributeCache());
        // Create the backend and store
        var backend = new Backend(resolver);
        var store = new DatomStore(services.GetRequiredService<ILogger<DatomStore>>(), settings, backend);
        // Find the Query Engine
        var engine = services.GetRequiredService<IQueryEngine>();
        // Create the connection, but don't start it yet
        var connection = new Connection(services.GetRequiredService<ILogger<Connection>>(), store, services, (QueryEngine)engine, settings);
        // Set the connection on the store and bootstrap it
        store.Connection = connection;
        store.Bootstrap();
        // Bootstrap the connection, after this we're live and good to go
        connection.Bootstrap();
        return connection;
    }
}
