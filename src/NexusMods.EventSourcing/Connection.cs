using System;
using System.Collections;
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
    private readonly IDictionary<ulong, ulong> _entityRemaps = new Dictionary<ulong, ulong>();

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
        _entityRemaps.Clear();
        Store.Transact(ref socket, ref _nextTx, _entityRemaps);
        _nextTx++;
        _nextEntity = socket.EntityStart;

        var remapped = new Dictionary<ulong, ulong>();
        foreach (var (key, value) in _entityRemaps)
        {
            remapped.Add(key, value);
        }

        return new TransactionResult(new Db(this, TransactionId.From(thisTx)), remapped);
    }

    public IDb Dref()
    {
        return new Db(this, TransactionId.From(_nextTx));
    }

    private class TransactionSinkSocket(ulong entityStart, ulong tx, IEntityRegistry registry, IDictionary<EntityId, AEntity> attachedEntities, IReadOnlyCollection<(ulong E, ulong A, object v)> changes) : IDatomSinkSocket
    {
        public ulong EntityStart => entityStart;
        public void Process<TSink>(ref TSink sink) where TSink : IDatomSink
        {
            foreach (var entity in attachedEntities.Values)
            {
                var entityId = entity.Id.Value;
                registry.EmitOne(sink, entityId, entity, tx);
            }
        }
    }
}
