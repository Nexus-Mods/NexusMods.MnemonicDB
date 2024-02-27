using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage;

public class DatomStore : IDatomStore
{
    private static readonly UInt128 RootKey = "5C68CD24-A4BF-42EA-8892-6BF24956BE74".ToUInt128Guid();
    private readonly IKvStore _kvStore;
    private readonly AttributeRegistry _registry;
    private RootNode _rootNode = null!;
    private readonly PooledMemoryBufferWriter _pooledWriter;
    private ulong _txId;


    public DatomStore(IKvStore kvStore, AttributeRegistry registry)
    {
        _kvStore = kvStore;
        _registry = registry;
        _pooledWriter = new PooledMemoryBufferWriter();
        _txId = Ids.MinId(Ids.Partition.Tx);
        Bootstrap();
    }

    /// <summary>
    /// Sets up the initial state of the store.
    /// </summary>
    private void Bootstrap()
    {
        _rootNode = new RootNode(_registry);
        Transact(BuiltInAttributes.InitialDatoms);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
    public TxId Transact(IEnumerable<ITypedDatom> datoms)
    {
        throw new NotImplementedException();
    }

    public IIterator<IRawDatom> Where<TAttr>(TxId txId) where TAttr : IAttribute
    {
        throw new NotImplementedException();
    }

    public IEntityIterator EntityIterator(TxId txId)
    {
        throw new NotImplementedException();
    }

    public void RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        throw new NotImplementedException();
    }

    public Expression GetValueReadExpression(Type attribute, Expression valueSpan, out ulong attributeId)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<EntityId> ReverseLookup<TAttribute>(TxId txId) where TAttribute : IAttribute<EntityId>
    {
        throw new NotImplementedException();
    }
}
