using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class Db(IDatomStore store, IConnection connection, TxId txId) : IDb
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
}
