using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.Storage.RocksDbBackend;
using NexusMods.EventSourcing.TestModel.Helpers;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests;

public class AEventSourcingTest : IAsyncLifetime
{
    private readonly IAttribute[] _attributes;
    private readonly IServiceProvider _provider;
    private readonly AttributeRegistry _registry;
    private readonly IValueSerializer[] _valueSerializers;
    private Backend _backend;

    private DatomStore _store;
    protected Connection Connection = null!;
    protected ILogger Logger;


    protected AEventSourcingTest(IServiceProvider provider)
    {
        _provider = provider;
        _valueSerializers = provider.GetRequiredService<IEnumerable<IValueSerializer>>().ToArray();
        _attributes = provider.GetRequiredService<IEnumerable<IAttribute>>().ToArray();

        _registry = new AttributeRegistry(_valueSerializers, _attributes);

        Config = new DatomStoreSettings
        {
            Path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
                .Combine("tests_eventsourcing" + Guid.NewGuid())
        };
        _backend = new Backend(_registry);

        _store = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), _registry, Config, _backend);

        Logger = provider.GetRequiredService<ILogger<AEventSourcingTest>>();
    }

    protected DatomStoreSettings Config { get; set; }

    protected SettingsTask VerifyModel<TReadModel>(TReadModel model)
        where TReadModel : IReadModel
    {
        var datoms = DatomsFor(model).ToTable(_registry);
        return Verify(datoms);
    }

    protected SettingsTask VerifyModel(IEnumerable<IReadModel> model)
    {
        var datoms = model.SelectMany(DatomsFor)
            .ToTable(_registry);
        return Verify(datoms);
    }

    public async Task InitializeAsync()
    {
        await _store.Sync();

        Connection = await Connection.Start(_store, _valueSerializers, _attributes);
    }

    protected IReadDatom[] DatomsFor(IReadModel model)
    {
        var fromAttributes = model.GetType()
            .GetProperties()
            .SelectMany(p => p.CustomAttributes)
            .Select(p => p.AttributeType)
            .Where(a => a.IsAssignableTo(typeof(IFromAttribute)))
            .Select(f => f.GenericTypeArguments.First())
            .ToArray();

        var datoms = model.Db.Datoms(model.Id)
            .Where(d => fromAttributes.Contains(d.AttributeType))
            .ToArray();
        return datoms;
    }

    protected SettingsTask VerifyTable(IEnumerable<IReadDatom> datoms)
    {
        return Verify(datoms.ToTable(_registry));
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
