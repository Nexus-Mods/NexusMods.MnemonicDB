using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing;

internal class Db(IDatomStore store, Connection connection, TxId txId) : IDb
{
    public TxId BasisTxId => txId;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids) where TModel : IReadModel
    {
        using var iterator = store.EntityIterator(txId);
        var reader = connection.ModelReflector.GetReader<TModel>();
        foreach (var id in ids)
        {
            iterator.Set(id);
            var model = reader(id, iterator, this);
            yield return model;
        }
    }

    public TModel Get<TModel>(EntityId id) where TModel : IReadModel
    {
        using var iterator = store.EntityIterator(txId);
        iterator.Set(id);
        var reader = connection.ModelReflector.GetReader<TModel>();
        return reader(id, iterator, this);
    }

    /// <inheritdoc />
    public IEnumerable<TModel> GetReverse<TAttribute, TModel>(EntityId id) where TAttribute : IAttribute<EntityId> where TModel : IReadModel
    {
        var iterator = store.ReverseLookup<TAttribute>(txId);
        using var entityIterator = store.EntityIterator(txId);
        var reader = connection.ModelReflector.GetReader<TModel>();
        foreach (var entityId in iterator)
        {
            entityIterator.Set(entityId);
            var model = reader(entityId, entityIterator, this);
            yield return model;
        }
    }
}
