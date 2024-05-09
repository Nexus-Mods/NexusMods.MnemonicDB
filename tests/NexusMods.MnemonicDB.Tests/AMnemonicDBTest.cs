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

    protected SettingsTask VerifyModel<TReadModel>(TReadModel model)
        where TReadModel : IModel
    {
        var fromAttributes = EntityToDictionary(model);
        return Verify(fromAttributes);
    }

    private Dictionary<string, string> EntityToDictionary<TReadModel>(TReadModel model) where TReadModel : IModel
    {
        throw new NotImplementedException();
        /*
        return new Dictionary<string, string>(from prop in model.GetType().GetProperties()
            where prop.Name != "Id" && prop.Name != "Tx" && prop.Name != "Db"
            let value = Stringify(prop.GetValue(model)!)
            where value != null
            select new KeyValuePair<string, string>(prop.Name, value));
            */
    }

    protected SettingsTask VerifyModel<T>(IEnumerable<T> models)
    where T : IModel
    {
        return Verify(models.Select(EntityToDictionary).ToArray());
    }

    private string Stringify(object value)
    {
        if (value is IEntity entity)
            return entity.Id.Value.ToString("x");
        return value!.ToString() ?? "";
    }

    protected SettingsTask VerifyTable(IEnumerable<IReadDatom> datoms)
    {
        return Verify(datoms.ToTable(_registry));
    }

    protected async Task<ILoadout> InsertExampleData()
    {
        var tx = Connection.BeginTransaction();
        var loadout = tx.New<ILoadout>();
        loadout.Name = "Test Loadout";

        List<IMod> mods = new();

        foreach (var modName in new[] { "Mod1", "Mod2", "Mod3" })
        {
            var mod = tx.New<IMod>();
            mod.Name = modName;
            mod.Source = new Uri("http://somesite.com/" + modName);
            mod.Loadout = loadout;

            var idx = 0;
            foreach (var path in new[] { "File1", "File2", "File3" })
            {
                var file = tx.New<IFile>();
                file.Path = path;
                file.Mod = mod;
                file.Size = Size.From((ulong)idx);
                file.Hash = Hash.From((ulong)(0xDEADBEEF + idx));
                idx += 1;
                mods.Add(mod);
            }
        }

        await tx.Commit();

        var tx2 = Connection.BeginTransaction();
        foreach (var mod in loadout.Mods)
        {
            var editable = tx.Edit(mod);
            editable.Name = mod.Name + " - Updated";
        }
        var result = await tx2.Commit();

        return result.Remap(loadout);
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
