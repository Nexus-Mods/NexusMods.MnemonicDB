using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly IDatomStore _store;
    private IDb _db = null!;
    private readonly Task _startupTask;

    /// <summary>
    ///     Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(ILogger<Connection> logger, IDatomStore store, IServiceProvider provider, IEnumerable<IAttribute> declaredAttributes)
    {
        ServiceProvider = provider;
        _store = store;
        // Async startup routines, we'll deref this task when we interact with the store
        _startupTask = Task.Run(async () =>
        {
            try
            {
                var storeResult = await AddMissingAttributes(declaredAttributes);
                _db = new Db(storeResult.Snapshot, this, storeResult.AssignedTxId, (AttributeRegistry)_store.Registry);
                _store.TxLog.Subscribe(log =>
                {
                    _db = new Db(log.Snapshot, this, log.TxId, (AttributeRegistry)_store.Registry);
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add missing attributes");
            }
        });
    }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; set; }

    /// <inheritdoc />
    public IDb Db
    {
        get
        {
            if (!_startupTask.IsCompleted)
                _startupTask.Wait();
            return _db;
        }
    }


    /// <inheritdoc />
    public TxId TxId => _store.AsOfTxId;

    /// <inheritdoc />
    public IDb AsOf(TxId txId)
    {
        if (!_startupTask.IsCompleted)
            _startupTask.Wait();
        var snapshot = new AsOfSnapshot(_store.GetSnapshot(), txId, (AttributeRegistry)_store.Registry);
        return new Db(snapshot, this, txId, (AttributeRegistry)_store.Registry);
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction()
    {
        if (!_startupTask.IsCompleted)
            _startupTask.Wait();
        return new Transaction(this, _store.Registry);
    }

    /// <inheritdoc />
    public IObservable<IDb> Revisions => _store.TxLog
        .Select(log => new Db(log.Snapshot, this, log.TxId, (AttributeRegistry)_store.Registry));


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
        var start = BuiltInAttributes.UniqueIdEntityId;
        var attrIds = snapshot.Datoms(IndexType.AEVTCurrent, start, AttributeId.From((ushort)(start.Value + 1)))
            .Select(d => d.E);

        foreach (var attrId in attrIds)
        {
            var serializerId = ValueTags.Null;
            var uniqueId = Symbol.Unknown;

            var from = new KeyPrefix().Set(attrId, AttributeId.Min, TxId.MinValue, false);
            var to = new KeyPrefix().Set(attrId, AttributeId.Max, TxId.MaxValue, false);

            foreach (var rawDatom in snapshot.Datoms(IndexType.EAVTCurrent, from, to))
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
        if (!_startupTask.IsCompleted)
            await _startupTask;
        StoreResult newTx;
        
        if (txFunctions == null)
            newTx = await _store.Transact(datoms, txFunctions);
        else
            newTx = await _store.Transact(datoms, txFunctions, snapshot => new Db(snapshot, this, TxId, (AttributeRegistry)_store.Registry));

        var result = new CommitResult(new Db(newTx.Snapshot, this, newTx.AssignedTxId, (AttributeRegistry)_store.Registry)
            , newTx.Remaps);
        return result;
    }
}
