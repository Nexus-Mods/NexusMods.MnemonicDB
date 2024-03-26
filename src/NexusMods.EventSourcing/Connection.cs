using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.Storage;

namespace NexusMods.EventSourcing;

/// <summary>
/// Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly object _lock = new();
    private ulong _nextEntityId = Ids.MinId(Ids.Partition.Entity);
    private readonly IDatomStore _store;
    internal readonly ModelReflector<Transaction> ModelReflector;

    /// <summary>
    /// Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    private Connection(IDatomStore store)
    {
        _store = store;
        ModelReflector = new ModelReflector<Transaction>(store);
    }

    /// <summary>
    /// Creates and starts a new connection, some setup and reflection is done here so it is async
    /// </summary>
    public static async Task<Connection> Start(IDatomStore store, IEnumerable<IValueSerializer> serializers, IEnumerable<IAttribute> declaredAttributes)
    {
        var conn = new Connection(store);
        await conn.AddMissingAttributes(serializers, declaredAttributes);
        return conn;
    }

    /// <summary>
    /// Creates and starts a new connection, some setup and reflection is done here so it is async
    /// </summary>
    public static async Task<Connection> Start(IServiceProvider provider)
    {
        var db = provider.GetRequiredService<IDatomStore>();
        await db.Sync();
        return await Start(provider.GetRequiredService<IDatomStore>(), provider.GetRequiredService<IEnumerable<IValueSerializer>>(), provider.GetRequiredService<IEnumerable<IAttribute>>());
    }


    private async Task AddMissingAttributes(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> declaredAttributes)
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
        var db = Db.Datoms<BuiltInAttributes.UniqueId>(IndexType.AEVTCurrent);
        var attrIds = _store.GetEntitiesWithAttribute<BuiltInAttributes.UniqueId>(TxId.MaxValue);

        foreach (var attr in attrIds)
        {
            var serializerId = Symbol.Unknown;
            var uniqueId = Symbol.Unknown;

            foreach (var datom in _store.GetAttributesForEntity(attr, TxId.MaxValue))
            {
                switch (datom)
                {
                    case BuiltInAttributes.ValueSerializerId.ReadDatom serializerIdDatom:
                        serializerId = serializerIdDatom.V;
                        break;
                    case BuiltInAttributes.UniqueId.ReadDatom uniqueIdDatom:
                        uniqueId = uniqueIdDatom.V;
                        break;
                }
            }
            yield return new DbAttribute(uniqueId, AttributeId.From(attr.Value), serializerId);
        }
    }




    /// <inheritdoc />
    public IDb Db => new Db(_store.GetSnapshot(), this, TxId);


    /// <inheritdoc />
    public TxId TxId => _store.AsOfTxId;


    /// <inheritdoc />
    public async Task<ICommitResult> Transact(IEnumerable<IWriteDatom> datoms)
    {
        var newTx = await _store.Transact(datoms);
        var result = new CommitResult(newTx.AssignedTxId, newTx.Remaps);
        return result;
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction()
    {
        return new Transaction(this);
    }

    /// <inheritdoc />
    public IObservable<(TxId TxId, IReadOnlyCollection<IReadDatom> Datoms)> Commits => _store.TxLog;

    /// <inheritdoc />
    public T GetActive<T>(EntityId id) where T : IActiveReadModel
    {
        var db = Db;
        var ctor = ModelReflector.GetActiveModelConstructor<T>();
        var activeModel = (T)ctor(db, id);
        return activeModel;
    }
}
