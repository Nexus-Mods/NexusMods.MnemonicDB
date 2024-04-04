using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Comparators;
using NexusMods.MnemonicDB.Storage;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly AttributeRegistry _registry;

    private readonly IndexSegmentCache<EntityId> _entityCache;
    private readonly IndexSegmentCache<(EntityId, Type)> _reverseCache;

    public ISnapshot Snapshot { get; }
    public IAttributeRegistry Registry => _registry;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        _registry = registry;
        _connection = connection;
        _entityCache = new IndexSegmentCache<EntityId>(EntityDatoms, registry);
        _reverseCache = new IndexSegmentCache<(EntityId, Type)>(ReverseDatoms, registry);
        Snapshot = snapshot;
        BasisTxId = txId;
    }

    private static IEnumerable<Datom> EntityDatoms(IDb db, EntityId id)
    {
        return db.Snapshot.Datoms(IndexType.EAVTCurrent, id, EntityId.From(id.Value + 1));
    }

    private static IEnumerable<Datom> ReverseDatoms(IDb db, (EntityId, Type) key)
    {
        var (id, type) = key;
        var attrId = db.Registry.GetAttributeId(type);

        Span<byte> startKey = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];
        Span<byte> endKey = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];
        MemoryMarshal.Write(startKey,  new KeyPrefix().Set(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false));
        MemoryMarshal.Write(endKey,  new KeyPrefix().Set(EntityId.MaxValueNoPartition, attrId, TxId.MaxValue, false));

        MemoryMarshal.Write(startKey.SliceFast(KeyPrefix.Size), id);
        MemoryMarshal.Write(endKey.SliceFast(KeyPrefix.Size), id.Value);


        return db.Snapshot.Datoms(IndexType.VAETCurrent, startKey, endKey);
    }

    public TxId BasisTxId { get; }

    public IConnection Connection => _connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids)
        where TModel : IEntity
    {
        foreach (var id in ids)
        {
            yield return Get<TModel>(id);
        }
    }

    public TValue Get<TAttribute, TValue>(EntityId id)
        where TAttribute : IAttribute<TValue>
    {
        var attrId = _registry.GetAttributeId<TAttribute>();
        var entry = _entityCache.Get(this, id);
        for (var i = 0; i < entry.Count; i++)
        {
            var datom = entry[i];
            if (datom.A == attrId)
            {
                return datom.Resolve<TValue>();
            }
        }

        throw new KeyNotFoundException();
    }

    public IEnumerable<TValue> GetAll<TAttribute, TValue>(EntityId id)
        where TAttribute : IAttribute<TValue>
    {
        var attrId = _registry.GetAttributeId<TAttribute>();
        var results = _entityCache.Get(this, id)
            .Where(d => d.A == attrId)
            .Select(d => d.Resolve<TValue>());

        return results;
    }

    public TModel Get<TModel>(EntityId id)
        where TModel : IEntity
    {
        return EntityConstructors<TModel>.Constructor(id, this);
    }




    public TModel[] GetReverse<TAttribute, TModel>(EntityId id)
        where TAttribute : IAttribute<EntityId>
        where TModel : IEntity
    {
        return _reverseCache.Get(this, (id, typeof(TAttribute)))
            .Select(d => d.E)
            .Select(Get<TModel>)
            .ToArray();
    }

    public IEnumerable<IReadDatom> Datoms(EntityId entityId)
    {
        return _entityCache.Get(this, entityId)
            .Select(d => d.Resolved);
    }

    public IEnumerable<IReadDatom> Datoms(TxId txId)
    {
        return Snapshot.Datoms(IndexType.TxLog, txId, TxId.From(txId.Value + 1))
            .Select(d => d.Resolved);
    }

    public void Dispose() { }
}
