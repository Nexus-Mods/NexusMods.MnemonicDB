using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.Storage;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly AttributeRegistry _registry;
    private readonly EntityCache _cache;
    private readonly ConcurrentDictionary<(EntityId, Type), EntityId[]> _reverseCache = new();
    internal readonly ISnapshot Snapshot;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        _registry = registry;
        _connection = connection;
        _cache = new EntityCache(this, registry);
        Snapshot = snapshot;
        BasisTxId = txId;
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
        return _cache.Get<TAttribute, TValue>(ref header);
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
        if (!_reverseCache.TryGetValue((id, typeof(TAttribute)), out var eIds))
        {
            using var attrSource = Snapshot.GetIterator(IndexType.VAETCurrent);
            var attrId = _registry.GetAttributeId<TAttribute>();
            eIds = attrSource
                .SeekTo(attrId, id)
                .WhileUnmanagedV(id)
                .While(attrId)
                .Select(c => c.CurrentKeyPrefix().E)
                .ToArray();

            _reverseCache[(id, typeof(TAttribute))] = eIds;
        }

        var results = GC.AllocateUninitializedArray<TModel>(eIds.Length);
        for (var i = 0; i < eIds.Length; i++)
            results[i] = Get<TModel>(eIds[i]);
        return results;
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

    public IEnumerable<TValueType> GetAll<TAttribute, TValueType>(ref ModelHeader model, EntityId modelId)
        where TAttribute : IAttribute<TValueType>
    {
        return _cache.GetAll<TAttribute, TValueType>(ref model);
    }

    public void Dispose() { }
}
