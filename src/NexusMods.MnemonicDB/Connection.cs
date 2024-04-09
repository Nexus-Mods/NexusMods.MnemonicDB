using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly IDatomStore _store;
    private readonly Task _startupTask;

    /// <summary>
    ///     Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(ILogger<Connection> logger, IDatomStore store, IEnumerable<IValueSerializer> serializers, IEnumerable<IAttribute> declaredAttributes)
    {
        _store = store;
        // Async startup routines, we'll deref this task when we interact with the store
        _startupTask = Task.Run(async () =>
        {
            try
            {
                await AddMissingAttributes(serializers, declaredAttributes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add missing attributes");
            }
        });
    }


    /// <inheritdoc />
    public IDb Db
    {
        get
        {
            if (!_startupTask.IsCompleted)
                _startupTask.Wait();
            return new Db(_store.GetSnapshot(), this, TxId, (AttributeRegistry)_store.Registry);
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
        return new Transaction(this);
    }

    /// <inheritdoc />
    public IObservable<IDb> Revisions => _store.TxLog
        .Select(log => new Db(log.Snapshot, this, log.TxId, (AttributeRegistry)_store.Registry));


    private async Task AddMissingAttributes(IEnumerable<IValueSerializer> valueSerializers,
        IEnumerable<IAttribute> declaredAttributes)
    {
        var serializerByType = valueSerializers.ToDictionary(s => s.NativeType);

        var existing = ExistingAttributes().ToDictionary(a => a.UniqueId);

        var missing = declaredAttributes.Where(a => !existing.ContainsKey(a.Id)).ToArray();
        if (missing.Length == 0)
        {
            _store.Registry.Populate(existing.Values.ToArray());
            return;
        }

        var newAttrs = new List<DbAttribute>();

        var attrId = existing.Values.Max(a => a.AttrEntityId).Value;
        foreach (var attr in missing)
        {
            var id = ++attrId;

            var serializer = serializerByType[attr.ValueType];
            var uniqueId = attr.Id;
            newAttrs.Add(new DbAttribute(uniqueId, AttributeId.From(id), serializer.UniqueId));
        }

        await _store.RegisterAttributes(newAttrs);
    }

    private IEnumerable<DbAttribute> ExistingAttributes()
    {
        var snapshot = _store.GetSnapshot();
        var start = BuiltInAttributes.UniqueIdEntityId;
        var attrIds = snapshot.Datoms(IndexType.AEVTCurrent, start, AttributeId.From(start.Value + 1))
            .Select(d => d.E);

        foreach (var attrId in attrIds)
        {
            var serializerId = Symbol.Unknown;
            var uniqueId = Symbol.Unknown;

            var from = new KeyPrefix().Set(attrId, AttributeId.Min, TxId.MinValue, false);
            var to = new KeyPrefix().Set(attrId, AttributeId.Max, TxId.MaxValue, false);

            foreach (var datom in snapshot.Datoms(IndexType.EAVTCurrent, from, to)
                         .Select(d => d.Resolved))
                switch (datom)
                {
                    case Attribute<BuiltInAttributes.ValueSerializerId, Symbol>.ReadDatom serializerIdDatom:
                        serializerId = serializerIdDatom.V;
                        break;
                    case Attribute<BuiltInAttributes.UniqueId, Symbol>.ReadDatom uniqueIdDatom:
                        uniqueId = uniqueIdDatom.V;
                        break;
                }

            yield return new DbAttribute(uniqueId, AttributeId.From(attrId.Value), serializerId);
        }
    }

    internal async Task<ICommitResult> Transact(IEnumerable<IWriteDatom> datoms)
    {
        if (!_startupTask.IsCompleted)
            await _startupTask;
        var newTx = await _store.Transact(datoms);
        var result = new CommitResult(new Db(newTx.Snapshot, this, newTx.AssignedTxId, (AttributeRegistry)_store.Registry)
            , newTx.Remaps);
        return result;
    }
}
