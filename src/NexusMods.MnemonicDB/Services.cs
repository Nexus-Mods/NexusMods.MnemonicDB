using Microsoft.Extensions.DependencyInjection;
using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;
using NexusMods.HyperDuck.BindingConverters;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.BindingConverters;
using NexusMods.MnemonicDB.QueryFunctions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Extension methods for adding attributes and other types to the service collection.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Registers the event sourcing services with the service collection.
    /// </summary>
    public static IServiceCollection AddMnemonicDB(this IServiceCollection s) =>
        s.AddSingleton<IConnectionFactory, ConnectionFactory>()
            .AddSingleton<IQueryEngine, QueryEngine>()
            .AddSingleton<AScalarFunction, ToStringScalarFn>()
            .AddConverters()
            .AddMnemonicDBStorage();

    private static IServiceCollection AddConverters(this IServiceCollection s)
    {
        s.AddSingleton<IBindingConverter, DbBindingConverter>();
        s.AddSingleton<IBindingConverter, ConnectionBindingConverter>();
        s.AddBindingConverter<EntityId, ulong>(static e => e.Value);
        s.AddBindingConverter<Hash, ulong>(static h => h.Value);
        s.AddBindingConverter<Size, ulong>(static size => size.Value);
        s.AddBindingConverter<RelativePath, string>(static p => p.ToString());
        
        s.AddValueAdaptor<ulong, EntityId>(static v => EntityId.From(v));
        s.AddValueAdaptor<ulong, TxId>(static v => TxId.From(v));
        s.AddValueAdaptor<ulong, Hash>(static v => Hash.From(v));
        s.AddValueAdaptor<ulong, Size>(static v => Size.From(v));
        s.AddValueAdaptor<long, Size>(static v => Size.FromLong(v));
        s.AddValueAdaptor<StringElement, RelativePath>(static sel => RelativePath.FromUnsanitizedInput(sel.GetString()));

        return s;
    }
    
    /// <summary>
    /// Adds the MnemonicDB storage services to the service collection.
    /// </summary>
    private static IServiceCollection AddMnemonicDBStorage(this IServiceCollection services)
    {
        services.AddAttributeDefinitionModel()
            .AddAdapters()
            .AddTransactionModel();
        return services;
    }
}
