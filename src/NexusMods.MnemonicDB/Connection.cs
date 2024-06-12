﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection, IHostedService
{
    private readonly IDatomStore _store;
    private readonly Dictionary<Symbol, IAttribute> _declaredAttributes;
    private readonly ILogger<Connection> _logger;
    private Task? _bootstrapTask;

    private BehaviorSubject<Revision> _dbStream;
    private IDisposable? _dbStreamDisposable;

    /// <summary>
    ///     Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(ILogger<Connection> logger, IDatomStore store, IServiceProvider provider, IEnumerable<IAttribute> declaredAttributes)
    {
        ServiceProvider = provider;
        _logger = logger;
        _declaredAttributes = declaredAttributes.ToDictionary(a => a.Id);
        _store = store;
        _dbStream = new BehaviorSubject<Revision>(default!);
    }

    /// <summary>
    /// Scrubs the transaction stream so that we only ever move forward and never repeat transactions
    /// </summary>
    private static IObservable<(TxId TxId, ISnapshot Snapshot)> ForwardOnly(IObservable<(TxId txId, ISnapshot snapshot)> dbStream)
    {
        TxId? prev = null;

        return Observable.Create((IObserver<(TxId txId, ISnapshot snapshot)> observer) =>
        {
            return dbStream.Subscribe((nextItem) =>
            {
                var (nextTxId, _) = nextItem;
                if (prev != null && prev.Value >= nextTxId)
                    return;

                observer.OnNext(nextItem);
                prev = nextTxId;
            }, observer.OnError, observer.OnCompleted);
        });
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; set; }

    /// <inheritdoc />
    public IDb Db
    {
        get
        {
            var val = _dbStream;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (val == null)
                ThrowNullDb();
            return val!.Value.Database;
        }
    }

    /// <inheritdoc />
    public IAttributeRegistry Registry => _store.Registry;

    private static void ThrowNullDb()
    {
        throw new InvalidOperationException("Connection not started, did you forget to start the hosted service?");
    }


    /// <inheritdoc />
    public TxId TxId => _store.AsOfTxId;

    /// <inheritdoc />
    public IDb AsOf(TxId txId)
    {
        var snapshot = new AsOfSnapshot(_store.GetSnapshot(), txId, (AttributeRegistry)_store.Registry);
        return new Db(snapshot, this, txId, (AttributeRegistry)_store.Registry);
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction()
    {
        return new Transaction(this, _store.Registry);
    }

    /// <inheritdoc />
    public IObservable<Revision> Revisions
    {
        get
        {
            if (_dbStream == default!)
                ThrowNullDb();
            return _dbStream!;
        }
    }

    private async Task<StoreResult> AddMissingAttributes(IEnumerable<IAttribute> declaredAttributes)
    {

        var existing = ExistingAttributes().ToDictionary(a => a.UniqueId);
        if (existing.Count == 0)
            throw new AggregateException(
                "No attributes found in the database, something went wrong, as it should have been bootstrapped by now");

        var missing = declaredAttributes.Where(a => !existing.ContainsKey(a.Id)).ToArray();
        if (missing.Length == 0)
        {
            // Nothing new to assert, so just add the new data to the registry
            _store.Registry.Populate(existing.Values.ToArray());
            return await _store.Sync();
        }

        var newAttrs = new List<DbAttribute>();

        var attrId = existing.Values.Max(a => a.AttrEntityId).Value;
        foreach (var attr in missing)
        {
            var id = ++attrId;

            var uniqueId = attr.Id;
            newAttrs.Add(new DbAttribute(uniqueId, AttributeId.From(id), attr.LowLevelType, attr));
        }

        await _store.RegisterAttributes(newAttrs);
        return await _store.Sync();
    }

    private IEnumerable<DbAttribute> ExistingAttributes()
    {
        var db = new Db(_store.GetSnapshot(), this, TxId, (AttributeRegistry)_store.Registry);

        foreach (var attribute in AttributeDefinition.All(db))
        {
            var declared = _declaredAttributes[attribute.UniqueId];
            yield return new DbAttribute(attribute.UniqueId, AttributeId.From((ushort)attribute.Id.Value), attribute.ValueType, declared);
        }
    }

    internal async Task<ICommitResult> Transact(IndexSegment datoms, HashSet<ITxFunction>? txFunctions)
    {
        StoreResult newTx;

        if (txFunctions == null)
            newTx = await _store.Transact(datoms, txFunctions);
        else
            newTx = await _store.Transact(datoms, txFunctions, snapshot => new Db(snapshot, this, TxId, (AttributeRegistry)_store.Registry));

        var result = new CommitResult(new Db(newTx.Snapshot, this, newTx.AssignedTxId, (AttributeRegistry)_store.Registry)
            , newTx.Remaps);
        return result;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (this)
        {
            _bootstrapTask ??= Task.Run(Bootstrap, cancellationToken);
        }
        await _bootstrapTask;
    }

    private async Task Bootstrap()
    {
        // Won't complete until the DatomStore has properly started
        await _store.StartAsync(CancellationToken.None);
        try
        {
            var storeResult = await AddMissingAttributes(_declaredAttributes.Values);

            _dbStreamDisposable = ForwardOnly(_store.TxLog)
                .Select(log =>
                {
                    var db = new Db(log.Snapshot, this, log.TxId, (AttributeRegistry)_store.Registry);
                    var addedItems = db.Datoms(SliceDescriptor.Create(db.BasisTxId, _store.Registry));
                    return new Revision
                    {
                        Database = db,
                        AddedDatoms = addedItems
                    };
                })
                .Subscribe(_dbStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add missing attributes");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _dbStreamDisposable?.Dispose();
        return Task.CompletedTask;
    }
}
