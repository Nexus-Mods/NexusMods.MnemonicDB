using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing;

public class Db(IDatomStore store, Connection connection, TxId txId) : IDb
{
    public TxId BasisTxId => txId;


    public IIterator Where<TAttr>() where TAttr : IAttribute
    {
        return store.Where<TAttr>(txId);
    }

    public IIterator Where(EntityId id)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids) where TModel : IReadModel
    {
        using var iterator = store.EntityIterator(txId);
        var reader = connection.ModelReflector.GetReader<TModel>();
        foreach (var id in ids)
        {
            iterator.Set(id);
            var model = reader(id, iterator);
            yield return model;
        }
    }
}
