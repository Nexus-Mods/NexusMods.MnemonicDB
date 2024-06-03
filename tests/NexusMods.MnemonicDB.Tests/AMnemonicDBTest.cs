using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using Xunit.Sdk;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class AMnemonicDBTest : IDisposable, IAsyncLifetime
{
    private readonly IAttribute[] _attributes;
    private readonly IServiceProvider _provider;
    private AttributeRegistry _registry;
    private Backend _backend;

    private DatomStore _store;
    protected IConnection Connection;
    protected ILogger Logger;


    protected AMnemonicDBTest(IServiceProvider provider)
    {
        _provider = provider;
        _attributes = provider.GetRequiredService<IEnumerable<IAttribute>>().ToArray();

        _registry = new AttributeRegistry(_attributes);

        Config = new DatomStoreSettings
        {
            Path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
                .Combine("tests_MnemonicDB" + Guid.NewGuid())
        };
        _backend = new Backend(_registry);

        _store = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), _registry, Config, _backend);
        Connection = new Connection(provider.GetRequiredService<ILogger<Connection>>(), _store, provider, _attributes);

        Logger = provider.GetRequiredService<ILogger<AMnemonicDBTest>>();
    }

    protected DatomStoreSettings Config { get; set; }

    protected SettingsTask VerifyModel<T>(T model)
    where T : IEnumerable<IReadDatom>
    {
        return VerifyTable(model);
    }

    protected SettingsTask VerifyModel<T>(IEnumerable<T> models)
    where T : IEnumerable<IReadDatom>
    {
        return VerifyTable(models.SelectMany(e => e));
    }

    protected SettingsTask VerifyTable(IEnumerable<IReadDatom> datoms)
    {
        return Verify(datoms.ToTable(_registry));
    }

    protected async Task<Loadout.ReadOnly> InsertExampleData()
    {

        var tx = Connection.BeginTransaction();
        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout"
        };
        List<Mod.New> mods = new();

        foreach (var modName in new[] { "Mod1", "Mod2", "Mod3" })
        {
            var mod = new Mod.New(tx)
            {
                Name = modName,
                Source = new Uri("http://somesite.com/" + modName),
                LoadoutId = loadout
            };

            var idx = 0;
            foreach (var file in new[] { "File1", "File2", "File3" })
            {
                _ = new File.New(tx)
                {
                    Path = file,
                    ModId = mod,
                    Size = Size.From((ulong)idx),
                    Hash = Hash.From((ulong)(0xDEADBEEF + idx))
                };
                idx += 1;
            }
            mods.Add(mod);
        }

        var txResult = await tx.Commit();
        var loadoutWritten = loadout.Remap(txResult);

        var tx2 = Connection.BeginTransaction();
        foreach (var mod in loadoutWritten.Mods)
        {
            tx2.Add(txResult[mod.Id], Mod.Name, mod.Name + " - Updated");
        }
        await tx2.Commit();

        return Loadout.As(Connection.Db, loadoutWritten.Id);
    }

    public void Dispose()
    {
        _registry.Dispose();
        _store.Dispose();
    }


    protected async Task RestartDatomStore()
    {
        _registry.Dispose();
        _store.Dispose();
        _backend.Dispose();

        GC.Collect();

        _backend = new Backend(_registry);
        _registry = new AttributeRegistry(_attributes);
        _store = new DatomStore(_provider.GetRequiredService<ILogger<DatomStore>>(), _registry, Config, _backend);

        Connection = new Connection(_provider.GetRequiredService<ILogger<Connection>>(), _store, _provider, _attributes);
        await InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        await _store.StartAsync(CancellationToken.None);
        await ((Connection)Connection).StartAsync(CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        // Nothing to do
        return Task.CompletedTask;
    }
}
