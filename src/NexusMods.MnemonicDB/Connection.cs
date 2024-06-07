using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
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
    private readonly IEnumerable<IAttribute> _declaredAttributes;
    private readonly ILogger<Connection> _logger;
    private Task? _bootstrapTask;

    private BehaviorSubject<IDb> _dbStream;
    private IDisposable? _dbStreamDisposable;

    /// <summary>
    ///     Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(ILogger<Connection> logger, IDatomStore store, IServiceProvider provider, IEnumerable<IAttribute> declaredAttributes)
    {
        ServiceProvider = provider;
        _logger = logger;
        _declaredAttributes = declaredAttributes;
        _store = store;
        _dbStream = new BehaviorSubject<IDb>(null!);
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; set; }

    /// <inheritdoc />
    public IDb Db
    {
        get
        {
            var val = _dbStream.Value;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (val == null)
                ThrowNullDb();
            return val!;
        }
    }

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
    public IObservable<IDb> Revisions
    {
        get
        {
            if (_dbStream == null)
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
            _store.Registry.Populate(existing.Values.ToArray());
            return await _store.Sync();
        }

        var newAttrs = new List<DbAttribute>();

        var attrId = existing.Values.Max(a => a.AttrEntityId).Value;
        foreach (var attr in missing)
        {
            var id = ++attrId;

            var uniqueId = attr.Id;
            newAttrs.Add(new DbAttribute(uniqueId, AttributeId.From(id), attr.LowLevelType));
        }

        await _store.RegisterAttributes(newAttrs);
        return await _store.Sync();
    }

    private IEnumerable<DbAttribute> ExistingAttributes()
    {
        var snapshot = _store.GetSnapshot();
        var sliceDescriptor =
            SliceDescriptor.Create(BuiltInAttributes.UniqueId, _store.Registry);

        var attrIds = snapshot.Datoms(sliceDescriptor)
            .Select(d => d.E);

        foreach (var attrId in attrIds)
        {
            var serializerId = ValueTags.Null;
            var uniqueId = Symbol.Unknown;

            var entityDescriptor = SliceDescriptor.Create(EntityId.From(attrId.Value), _store.Registry);
            foreach (var rawDatom in snapshot.Datoms(entityDescriptor))
            {
                var datom = rawDatom.Resolved;

                if (datom.A == BuiltInAttributes.ValueType && datom is Attribute<ValueTags, byte>.ReadDatom serializerIdDatom)
                    serializerId = serializerIdDatom.V;
                else if (datom.A == BuiltInAttributes.UniqueId && datom is Attribute<Symbol, string>.ReadDatom uniqueIdDatom)
                    uniqueId = uniqueIdDatom.V;
            }

            yield return new DbAttribute(uniqueId, AttributeId.From((ushort)attrId.Value), serializerId);
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
            var storeResult = await AddMissingAttributes(_declaredAttributes);

            _dbStreamDisposable = _store.TxLog
                .Select(log => new Db(log.Snapshot, this, log.TxId, (AttributeRegistry)_store.Registry))
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
