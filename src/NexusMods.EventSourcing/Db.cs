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

    public IEnumerable<IReadDatom> Datoms(EntityId entityId)
    {
        using var iterator = _snapshot.GetIterator(IndexType.EAVTCurrent);
        iterator.Seek(entityId, AttributeId.From(0), TxId.From(0));

        while (iterator.Valid)
        {
            var c = iterator.CurrentPrefix();
            if (c.E != entityId) break;
            yield return iterator.Resolve(_registry);
            iterator.Next();
        }
    }

    public IEnumerable<IReadDatom> Datoms<TAttribute>(IndexType type)
    where TAttribute : IAttribute
    {
        using var iterator = _snapshot.GetIterator(type);
        var attrId = _registry.GetAttributeId<TAttribute>();
        iterator.Seek(EntityId.From(0), attrId, TxId.From(0));

        while (iterator.Valid)
        {
            var c = iterator.CurrentPrefix();
            if (c.A != attrId) break;

            yield return iterator.Resolve(_registry);
            iterator.Next();
        }
    }

    public void Dispose()
    {
    }
}
