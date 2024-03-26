using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly TxId _txId;
    private readonly ISnapshot _snapshot;

    public Db(ISnapshot snapshot, Connection connection, TxId txId)
    {
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

    public void Dispose()
    {
    }
}
