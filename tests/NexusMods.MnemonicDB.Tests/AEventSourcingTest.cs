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
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class AMnemonicDBTest : IDisposable
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
        where TReadModel : IEntity
    {
        var fromAttributes = EntityToDictionary(model);
        return Verify(fromAttributes);
    }

    private Dictionary<string, string> EntityToDictionary<TReadModel>(TReadModel model) where TReadModel : IEntity
    {
        return new Dictionary<string, string>(from prop in model.GetType().GetProperties()
            where prop.Name != "Id" && prop.Name != "Tx" && prop.Name != "Db"
            let value = Stringify(prop.GetValue(model)!)
            where value != null
            select new KeyValuePair<string, string>(prop.Name, value));
    }

    protected SettingsTask VerifyModel<T>(IEnumerable<T> models)
    where T : IEntity
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

    protected async Task<Loadout.Model> InsertExampleData()
    {
        var tx = Connection.BeginTransaction();
        var loadout = new Loadout.Model(tx)
        {
            Name = "Test Loadout"
        };
        List<Mod.Model> mods = new();

        foreach (var modName in new[] { "Mod1", "Mod2", "Mod3" })
        {
            var mod = new Mod.Model(tx)
            {
                Name = modName,
                Source = new Uri("http://somesite.com/" + modName),
                Loadout = loadout
            };

            var idx = 0;
            foreach (var file in new[] { "File1", "File2", "File3" })
            {
                _ = new File.Model(tx)
                {
                    Path = file,
                    Mod = mod,
                    Size = Size.From((ulong)idx),
                    Hash = Hash.From((ulong)(0xDEADBEEF + idx))
                };
                idx += 1;
            }
            mods.Add(mod);
        }

        var txResult = await tx.Commit();

        loadout = txResult.Remap(loadout);

        var tx2 = Connection.BeginTransaction();
        foreach (var mod in loadout.Mods)
        {
            tx2.Add(mod.Id, Mod.Name, mod.Name + " - Updated");
        }
        await tx2.Commit();

        return Connection.Db.Get<Loadout.Model>(loadout.Id);


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
    }
}
