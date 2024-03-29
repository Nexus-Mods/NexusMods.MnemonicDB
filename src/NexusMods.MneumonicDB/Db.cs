﻿using System.Collections.Generic;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.Storage;

namespace NexusMods.MneumonicDB;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly AttributeRegistry _registry;
    private readonly ISnapshot _snapshot;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        _registry = registry;
        _connection = connection;
        _snapshot = snapshot;
        BasisTxId = txId;
    }

    public TxId BasisTxId { get; }

    public IConnection Connection => _connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids) where TModel : IReadModel
    {
        var reader = _connection.ModelReflector.GetReader<TModel>();
        using var readerSource = _snapshot.GetIterator(IndexType.EAVTCurrent);

        foreach (var e in ids)
        {
            // Inlining this code so that we can re-use the iterator means roughly a 25% speedup
            // in loading a large number of entities.
            using var enumerator = readerSource
                .SeekTo(e)
                .While(e)
                .Resolve()
                .GetEnumerator();
            yield return reader(e, enumerator, this);
        }
    }

    public TValue Get<TAttribute, TValue>(EntityId id) where TAttribute : IAttribute<TValue>
    {
        throw new System.NotImplementedException();
    }

    public TModel Get<TModel>(EntityId id) where TModel : IReadModel
    {
        var reader = _connection.ModelReflector.GetReader<TModel>();

        using var source = _snapshot.GetIterator(IndexType.EAVTCurrent);
        using var enumerator = source.SeekTo(id)
            .While(id)
            .Resolve()
            .GetEnumerator();
        return reader(id, enumerator, this);
    }

    /// <inheritdoc />
    public IEnumerable<TModel> GetReverse<TAttribute, TModel>(EntityId id) where TAttribute : IAttribute<EntityId>
        where TModel : IReadModel
    {
        using var attrSource = _snapshot.GetIterator(IndexType.VAETCurrent);
        var attrId = _registry.GetAttributeId<TAttribute>();
        var eIds = attrSource
            .SeekTo(attrId, id)
            .WhileUnmanagedV(id)
            .While(attrId)
            .Select(c => c.CurrentKeyPrefix().E);

        var reader = _connection.ModelReflector.GetReader<TModel>();
        using var readerSource = _snapshot.GetIterator(IndexType.EAVTCurrent);

        foreach (var e in eIds)
        {
            // Inlining this code so that we can re-use the iterator means roughly a 25% speedup
            // in loading a large number of entities.
            using var enumerator = readerSource
                .SeekTo(e)
                .While(e)
                .Resolve()
                .GetEnumerator();
            yield return reader(e, enumerator, this);
        }
    }

    public IEnumerable<IReadDatom> Datoms(EntityId entityId)
    {
        using var iterator = _snapshot.GetIterator(IndexType.EAVTCurrent);
        foreach (var datom in iterator.SeekTo(entityId)
                     .While(entityId)
                     .Resolve())
            yield return datom;
    }

    public IEnumerable<IReadDatom> Datoms(TxId txId)
    {
        using var iterator = _snapshot.GetIterator(IndexType.TxLog);
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
        using var iterator = _snapshot.GetIterator(IndexType.AEVTCurrent);
        foreach (var datom in iterator
                     .SeekTo(a)
                     .While(a)
                     .Resolve())
            yield return datom;
    }

    public IDatomSource Iterate(IndexType index)
    {
        return _snapshot.GetIterator(index);
    }

    public void Dispose() { }
}
