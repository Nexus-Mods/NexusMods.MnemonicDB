using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class Connection : IConnection
{
    internal readonly DuckDBDatomStore Store;
    private ulong _nextTx;
    internal readonly IEntityRegistry Registry;
    private ulong _nextEntity;

    public Connection(DuckDBDatomStore store, IEntityRegistry registry)
    {
        Registry = registry;
        Store = store;
        _nextTx = Ids.MinId(IdSpace.Tx) + 10;
        _nextEntity = Ids.MinId(IdSpace.Entity);
        Registry.PopulateAttributeIds(Store.GetDbAttributes());
        _nextTx = Registry.TransactAttributeChanges(Store, _nextTx);
    }

    public TransactionResult Commit(IDictionary<EntityId, AEntity> attachedEntities, IReadOnlyCollection<(ulong E, ulong A, object v)> changes)
    {
        var thisTx = _nextTx;
        var socket = new TransactionSinkSocket(_nextEntity, _nextTx, Registry, attachedEntities, changes);
        Store.Transact(ref socket);
        _nextTx++;
        _nextEntity = socket.EntityStart;

        return new TransactionResult(new Db(this, TransactionId.From(thisTx)), socket.EntityRemaps);
    }

    public IDb Dref()
    {
        return new Db(this, TransactionId.From(_nextTx));
    }

    private class TransactionSinkSocket(ulong entityStart, ulong tx, IEntityRegistry registry, IDictionary<EntityId, AEntity> attachedEntities, IReadOnlyCollection<(ulong E, ulong A, object v)> changes) : IDatomSinkSocket
    {
        public ulong EntityStart => entityStart;
        public Dictionary<ulong, ulong> EntityRemaps = new();
        public void Process<TSink>(ref TSink sink) where TSink : IDatomSink
        {
            foreach (var entity in attachedEntities.Values)
            {
                var entityId = entity.Id.Value;
                var writer = registry.MakeEmitter<TSink>(entity.GetType());
                if (Ids.IsIdOfSpace(entityId, IdSpace.Temp))
                {
                    if (!EntityRemaps.TryGetValue(entity.Id.Value, out var newId))
                    {
                        newId = ++entityStart;
                        EntityRemaps.Add(entity.Id.Value, newId);
                        entityId = newId;
                    }
                    else
                    {
                        entityId = newId;
                    }
                }

                registry.EmitOne(sink, entityId, entity, tx);
            }
        }
    }
}
