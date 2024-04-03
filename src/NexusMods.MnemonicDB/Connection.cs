using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

/// <summary>
///     Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly object _lock = new();
    private readonly IDatomStore _store;

    /// <summary>
    ///     Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    private Connection(IDatomStore store)
    {
        _store = store;
    }


    /// <inheritdoc />
    public IDb Db => new Db(_store.GetSnapshot(), this, TxId, (AttributeRegistry)_store.Registry);


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
        return new Transaction(this);
    }

    /// <inheritdoc />
    public IObservable<IDb> Revisions => _store.TxLog
        .Select(log => new Db(log.Snapshot, this, log.TxId, (AttributeRegistry)_store.Registry));

    /// <summary>
    ///     Creates and starts a new connection, some setup and reflection is done here so it is async
    /// </summary>
    public static async Task<Connection> Start(IDatomStore store, IEnumerable<IValueSerializer> serializers,
        IEnumerable<IAttribute> declaredAttributes)
    {
        var conn = new Connection(store);
        await conn.AddMissingAttributes(serializers, declaredAttributes);
        return conn;
    }

    /// <summary>
    ///     Creates and starts a new connection, some setup and reflection is done here so it is async
    /// </summary>
    public static async Task<Connection> Start(IServiceProvider provider)
    {
        var db = provider.GetRequiredService<IDatomStore>();
        await db.Sync();
        return await Start(provider.GetRequiredService<IDatomStore>(),
            provider.GetRequiredService<IEnumerable<IValueSerializer>>(),
            provider.GetRequiredService<IEnumerable<IAttribute>>());
    }


    private async Task AddMissingAttributes(IEnumerable<IValueSerializer> valueSerializers,
        IEnumerable<IAttribute> declaredAttributes)
    {
        var serializerByType = valueSerializers.ToDictionary(s => s.NativeType);

        var existing = ExistingAttributes().ToDictionary(a => a.UniqueId);

        var missing = declaredAttributes.Where(a => !existing.ContainsKey(a.Id)).ToArray();
        if (missing.Length == 0)
            return;

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
        var db = Db;
        var start = BuiltInAttributes.UniqueIdEntityId;
        var attrIds = db.Snapshot.Datoms(IndexType.AEVTCurrent, start, AttributeId.From(start.Value + 1))
            .Select(d => d.E);

        foreach (var attrId in attrIds)
        {
            var serializerId = Symbol.Unknown;
            var uniqueId = Symbol.Unknown;

            foreach (var datom in db.Datoms(attrId))
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


    /// <inheritdoc />
    public async Task<ICommitResult> Transact(IEnumerable<IWriteDatom> datoms)
    {
        var newTx = await _store.Transact(datoms);
        var result = new CommitResult(new Db(newTx.Snapshot, this, newTx.AssignedTxId, (AttributeRegistry)_store.Registry)
            , newTx.Remaps);
        return result;
    }
}
