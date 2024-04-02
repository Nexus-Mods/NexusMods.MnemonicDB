using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.Comparators;
using NexusMods.MneumonicDB.Storage;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly AttributeRegistry _registry;

    private readonly IndexSegmentCache<EntityId> _entityCache;
    private readonly IndexSegmentCache<(EntityId, Type)> _reverseCache;

    internal readonly ISnapshot Snapshot;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        _registry = registry;
        _connection = connection;
        _entityCache = new IndexSegmentCache<EntityId>(EntityIterator);
        _reverseCache = new IndexSegmentCache<(EntityId, Type)>(ReverseIterator);
        Snapshot = snapshot;
        BasisTxId = txId;
    }

    private static IIterator EntityIterator(Db db, EntityId id)
    {
        return db.Iterate(IndexType.EAVTCurrent).SeekTo(id).While(id);
    }

    private static IIterator ReverseIterator(Db db, (EntityId, Type) key)
    {
        var (entityId, type) = key;
        var attrId = db._registry.GetAttributeId(type);
        return db.Iterate(IndexType.VAETCurrent)
            .SeekTo(attrId, entityId)
            .WhileUnmanagedV(entityId)
            .While(attrId);
    }

    public TxId BasisTxId { get; }

    public IConnection Connection => _connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids)
        where TModel : struct, IEntity
    {
        foreach (var id in ids)
        {
            yield return Get<TModel>(id);
        }
    }

    public TValue Get<TAttribute, TValue>(ref ModelHeader header, EntityId id)
        where TAttribute : IAttribute<TValue>
    {
        var attrId = _registry.GetAttributeId<TAttribute>();
        var attr = _registry.GetAttribute(attrId);
        var iterator = _entityCache.Get(this, header.Id)
            .GetIterator<EntityCacheComparator<AttributeRegistry>, AttributeRegistry>(_registry)
            .SeekTo(attrId)
            .While(attrId);

        if (!iterator.Valid)
            throw new KeyNotFoundException();

        unsafe
        {
            ((IValueSerializer<TValue>)attr.Serializer).Read(iterator.Current.SliceFast(sizeof(KeyPrefix)),
                out var value);
            return value;
        }
    }

    public IEnumerable<TValue> GetAll<TAttribute, TValue>(ref ModelHeader model, EntityId modelId)
        where TAttribute : IAttribute<TValue>
    {
        var attrId = _registry.GetAttributeId<TAttribute>();
        var attr = _registry.GetAttribute(attrId);
        var iterator = _entityCache.Get(this, model.Id)
            .GetIterator<EntityCacheComparator<AttributeRegistry>, AttributeRegistry>(_registry)
            .SeekTo(attrId)
            .While(attrId);

        return GetAllInner<TValue>(iterator, attr);
    }

    private static IEnumerable<TValue> GetAllInner<TValue>(WhileA<IIterator> iterator, IAttribute attr)
    {
        while (iterator.Valid)
        {
            ((IValueSerializer<TValue>)attr.Serializer).Read(iterator.Current, out var value);
            yield return value;
            iterator.Next();
        }
    }


    public TModel Get<TModel>(EntityId id)
        where TModel : struct, IEntity
    {
        ModelHeader header = new()
        {
            Id = id,
            Db = this
        };

        return MemoryMarshal.CreateReadOnlySpan(ref header, 1)
            .CastFast<ModelHeader, TModel>()[0];
    }

    public TModel[] GetReverse<TAttribute, TModel>(EntityId id)
        where TAttribute : IAttribute<EntityId>
        where TModel : struct, IEntity
    {
        return _reverseCache.Get(this, (id, typeof(TAttribute)))
            .GetIterator<EntityCacheComparator<AttributeRegistry>, AttributeRegistry>(_registry)
            .Select(c => c.CurrentKeyPrefix().E)
            .Select(Get<TModel>)
            .ToArray();
    }

    public IEnumerable<IReadDatom> Datoms(EntityId entityId)
    {
        using var iterator = Snapshot.GetIterator(IndexType.EAVTCurrent);
        foreach (var datom in iterator.SeekTo(entityId)
                     .While(entityId)
                     .Resolve())
            yield return datom;
    }

    public IEnumerable<IReadDatom> Datoms(TxId txId)
    {
        using var iterator = Snapshot.GetIterator(IndexType.TxLog);
        foreach (var datom in iterator
                     .SeekTo(txId)
                     .While(txId)
                     .Resolve())
            yield return datom;
    }

    public IEnumerable<IReadDatom> Datoms<TAttribute>()
        where TAttribute : IAttribute
    {
        var a = _registry.GetAttributeId<TAttribute>();
        using var iterator = Snapshot.GetIterator(IndexType.AEVTCurrent);
        foreach (var datom in iterator
                     .SeekTo(a)
                     .While(a)
                     .Resolve())
            yield return datom;
    }

    public IDatomSource Iterate(IndexType index)
    {
        return Snapshot.GetIterator(index);
    }

    public void Dispose() { }
}
