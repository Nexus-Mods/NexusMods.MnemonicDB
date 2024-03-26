using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.Storage.Abstractions;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly TxId _txId;
    private readonly ISnapshot _snapshot;
    private readonly AttributeRegistry _registry;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        _registry = registry;
        _connection = connection;
        _snapshot = snapshot;
        _txId = txId;
    }

    public TxId BasisTxId => _txId;
    public IConnection Connection => _connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids) where TModel : IReadModel
    {
        throw new NotImplementedException();

        /*
        var reader = _connection.ModelReflector.GetReader<TModel>();
        foreach (var id in ids)
        {
            var iterator = store.GetAttributesForEntity(id, _txId).GetEnumerator();
            yield return reader(id, iterator, this);
        }*/
    }

    public TModel Get<TModel>(EntityId id) where TModel : IReadModel
    {
        var reader = _connection.ModelReflector.GetReader<TModel>();
        throw new NotImplementedException();
        /*
        return reader(id, store.GetAttributesForEntity(id, _txId).GetEnumerator(), this);*/
    }

    /// <inheritdoc />
    public IEnumerable<TModel> GetReverse<TAttribute, TModel>(EntityId id) where TAttribute : IAttribute<EntityId> where TModel : IReadModel
    {
        var reader = _connection.ModelReflector.GetReader<TModel>();
        throw new NotImplementedException();
        /*
        foreach (var entity in store.GetReferencesToEntityThroughAttribute<TAttribute>(id, _txId))
        {
            using var iterator = store.GetAttributesForEntity(entity, _txId).GetEnumerator();
            yield return reader(entity, iterator, this);
        }*/

    }

    public void Reload<TOuter>(TOuter aActiveReadModel) where TOuter : IActiveReadModel
    {
        var reader = _connection.ModelReflector.GetActiveReader<TOuter>();/*
        var iterator = store.GetAttributesForEntity(aActiveReadModel.Id, _txId).GetEnumerator();
        reader(aActiveReadModel, iterator);*/
    }

    public IEnumerable<IReadDatom> Datoms(IndexType type, EntityId? entityId, AttributeId? attributeId)
    {
        throw new NotImplementedException();

    }

    public IEnumerable<IReadDatom> Datoms<TAttribute>(IndexType type)
    where TAttribute : IAttribute
    {
        using var iterator = _snapshot.GetIterator(type);
        var key = new KeyPrefix();
        var attrId = _registry.GetAttributeId<TAttribute>();

        key.Set(EntityId.From(0), attrId, TxId.From(0), false);
        iterator.Seek(MemoryMarshal.CreateSpan(ref key, 1).CastFast<KeyPrefix, byte>());

        while (iterator.Valid)
        {
            var c = MemoryMarshal.Read<KeyPrefix>(iterator.Current);
            if (c.A != attrId) break;

            var datom = _registry.Resolve(c.E, c.A, iterator.Current, c.T, c.IsRetract);
            yield return datom;
            iterator.Next();
        }
    }

    public void Dispose()
    {
    }
}
