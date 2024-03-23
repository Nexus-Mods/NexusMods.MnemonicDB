using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing;

internal class Db(IDatomStore store, Connection connection, TxId txId) : IDb
{
    public TxId BasisTxId => txId;
    public IConnection Connection => connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids) where TModel : IReadModel
    {
        var reader = connection.ModelReflector.GetReader<TModel>();
        foreach (var id in ids)
        {
            var iterator = store.GetAttributesForEntity(id, txId).GetEnumerator();
            yield return reader(id, iterator, this);
        }
    }

    public TModel Get<TModel>(EntityId id) where TModel : IReadModel
    {
        var reader = connection.ModelReflector.GetReader<TModel>();
        return reader(id, store.GetAttributesForEntity(id, txId).GetEnumerator(), this);
    }

    /// <inheritdoc />
    public IEnumerable<TModel> GetReverse<TAttribute, TModel>(EntityId id) where TAttribute : IAttribute<EntityId> where TModel : IReadModel
    {
        var reader = connection.ModelReflector.GetReader<TModel>();
        foreach (var entity in store.GetReferencesToEntityThroughAttribute<TAttribute>(id, txId))
        {
            using var iterator = store.GetAttributesForEntity(entity, txId).GetEnumerator();
            yield return reader(entity, iterator, this);
        }

    }

    public void Reload<TOuter>(TOuter aActiveReadModel) where TOuter : IActiveReadModel
    {
        var reader = connection.ModelReflector.GetActiveReader<TOuter>();
        var iterator = store.GetAttributesForEntity(aActiveReadModel.Id, txId).GetEnumerator();
        reader(aActiveReadModel, iterator);
    }
}
