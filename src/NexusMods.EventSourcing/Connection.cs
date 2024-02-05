using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class Connection : IConnection
{
    private readonly DuckDBDatomStore _store;
    private ulong _nextTx;
    private readonly IEntityRegistry _registry;
    private ulong _nextEntity;

    public Connection(DuckDBDatomStore store, IEntityRegistry registry)
    {
        _registry = registry;
        _store = store;
        _nextTx = Ids.MinId(IdSpace.Tx) + 10;
        _nextEntity = Ids.MinId(IdSpace.Entity);
        _registry.PopulateAttributeIds(_store.GetDbAttributes());
        _nextTx = _registry.TransactAttributeChanges(_store, _nextTx);
    }

    public TransactionId Commit(IDictionary<EntityId, AEntity> attachedEntities, IReadOnlyCollection<(ulong E, ulong A, object v)> changes)
    {
        var socket = new TransactionSinkSocket(_nextEntity, _nextTx, _registry, attachedEntities, changes);
        _store.Transact(ref socket);
        _nextTx++;
        _nextEntity = socket.EntityStart;
        return TransactionId.From(_nextTx);
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
                    }
                    else
                    {
                        entityId = newId;
                    }
                    writer(entity, entityId, tx, ref sink);
                }
                else
                {
                    writer(entity, entity.Id.Value, tx, ref sink);
                }
            }
        }
    }
}
