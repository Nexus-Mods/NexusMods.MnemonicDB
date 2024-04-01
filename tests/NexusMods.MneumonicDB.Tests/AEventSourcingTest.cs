﻿using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.Storage;
using NexusMods.MneumonicDB.Storage.RocksDbBackend;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;
using NexusMods.MneumonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using File = NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels.File;

namespace NexusMods.MneumonicDB.Tests;

public class AMneumonicDBTest : IAsyncLifetime
{
    private readonly IAttribute[] _attributes;
    private readonly IServiceProvider _provider;
    private readonly AttributeRegistry _registry;
    private readonly IValueSerializer[] _valueSerializers;
    private Backend _backend;

    private DatomStore _store;
    protected Connection Connection = null!;
    protected ILogger Logger;


    protected AMneumonicDBTest(IServiceProvider provider)
    {
        _provider = provider;
        _valueSerializers = provider.GetRequiredService<IEnumerable<IValueSerializer>>().ToArray();
        _attributes = provider.GetRequiredService<IEnumerable<IAttribute>>().ToArray();

        _registry = new AttributeRegistry(_valueSerializers, _attributes);

        Config = new DatomStoreSettings
        {
            Path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
                .Combine("tests_MneumonicDB" + Guid.NewGuid())
        };
        _backend = new Backend(_registry);

        _store = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), _registry, Config, _backend);

        Logger = provider.GetRequiredService<ILogger<AMneumonicDBTest>>();
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
            where prop.PropertyType != typeof(ModelHeader)
            let value = Stringify(prop.GetValue(model)!)
            where value != null
            select new KeyValuePair<string, string>(prop.Name, value));
    }

    protected SettingsTask VerifyModel<T>(IEnumerable<T> models)
    where T : IEntity
    {
        return Verify(models.Select(EntityToDictionary).ToArray());
    }

    public async Task InitializeAsync()
    {
        await _store.Sync();

        Connection = await Connection.Start(_store, _valueSerializers, _attributes);
    }

    private string Stringify(object value)
    {
        if (value is IEntity entity)
            return entity.Header.Id.Value.ToString("x");
        return value!.ToString() ?? "";
    }

    protected SettingsTask VerifyTable(IEnumerable<IReadDatom> datoms)
    {
        return Verify(datoms.ToTable(_registry));
    }

    protected async Task<Loadout> InsertExampleData()
    {
        var tx = Connection.BeginTransaction();
        var loadout = new Loadout(tx)
        {
            Name = "Test Loadout"
        };
        List<Mod> mods = new();

        foreach (var modName in new[] { "Mod1", "Mod2", "Mod3" })
        {
            var mod = new Mod(tx)
            {
                Name = modName,
                Source = new Uri("http://somesite.com/" + modName),
                Loadout = loadout
            };

            var idx = 0;
            foreach (var file in new[] { "File1", "File2", "File3" })
            {
                _ = new File(tx)
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
            ModAttributes.Name.Add(tx2, mod.Header.Id, mod.Name + " - Updated");
        }
        await tx2.Commit();

        return Connection.Db.Get<Loadout>(loadout.Id);


    }

    public Task DisposeAsync()
    {
        _store.Dispose();
        return Task.CompletedTask;
    }


    protected async Task RestartDatomStore()
    {
        _store.Dispose();
        _backend.Dispose();


        _backend = new Backend(_registry);
        _store = new DatomStore(_provider.GetRequiredService<ILogger<DatomStore>>(), _registry, Config, _backend);
        await _store.Sync();

        Connection = await Connection.Start(_store, _valueSerializers, _attributes);
    }
}
