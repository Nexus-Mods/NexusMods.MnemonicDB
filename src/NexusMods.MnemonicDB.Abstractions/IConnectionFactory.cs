using System;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// A factory for creating connections from various sources.
/// </summary>
public interface IConnectionFactory
{

    /// <summary>
    /// Creates a connection from the given service provider, pulling the datom store settings from the service provider.
    /// </summary>
    public IConnection Create(IServiceProvider services);

    /// <summary>
    /// Creates a standard connection based on the given settings, and setting up the storage, query engine, and attribute resolver.
    /// </summary>
    public IConnection Create(IServiceProvider services, DatomStoreSettings settings);
}
