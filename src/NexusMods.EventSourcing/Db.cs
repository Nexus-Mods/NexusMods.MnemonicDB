using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class Db(IDatomStore store, IConnection connection, TxId txId, IDictionary<Type, IReadModelFactory> factories) : IDb
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
        var factory = factories[typeof(TModel)];

        var iterator = store.EntityIterator(txId);

        foreach (var id in ids)
        {
            var readModel = (TModel)factory.Create(id);
            iterator.SetEntityId(id);
            while (iterator.Next())
            {
                iterator.SetOn(readModel);
            }

            yield return readModel;
        }
    }
}
