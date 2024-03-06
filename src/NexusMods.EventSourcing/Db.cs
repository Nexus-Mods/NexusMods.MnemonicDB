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
            var iterator = store.Where(txId, id).GetEnumerator();
            yield return reader(id, iterator, this);
        }
    }

    public TModel Get<TModel>(EntityId id) where TModel : IReadModel
    {
        var reader = connection.ModelReflector.GetReader<TModel>();
        return reader(id, store.Where(txId, id).GetEnumerator(), this);
    }

    /// <inheritdoc />
    public IEnumerable<TModel> GetReverse<TAttribute, TModel>(EntityId id) where TAttribute : IAttribute<EntityId> where TModel : IReadModel
    {
        var reader = connection.ModelReflector.GetReader<TModel>();
        foreach (var entity in store.ReverseLookup<TAttribute>(txId, id))
        {
            using var iterator = store.Where(txId, entity).GetEnumerator();
            yield return reader(entity, iterator, this);
        }

    }

    public void Reload<TOuter>(TOuter aActiveReadModel) where TOuter : IActiveReadModel
    {
        var reader = connection.ModelReflector.GetActiveReader<TOuter>();
        var iterator = store.Where(txId, aActiveReadModel.Id).GetEnumerator();
        reader(aActiveReadModel, iterator);
    }
}
