using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions.BuiltinEntities;

namespace NexusMods.EventSourcing.Abstractions;

public interface IEntityRegistry
{
    public delegate void EmitEntity<TSink>(AEntity input, ulong e, ulong t, ref TSink sink) where TSink : IDatomSink;

    public delegate AEntity EntityReader<TResultSet>(TResultSet resultSet, IDb db) where TResultSet : IResultSet;

    public EmitEntity<TSink> MakeEmitter<TSink>(Type entityType) where TSink : IDatomSink;
    public EntityReader<TResultSet> MakeReader<TResultSet>(Type entityType) where TResultSet : IResultSet;

    public AEntity ReadOne<TResultSet>(ref TResultSet resultSet, IDb parentContext) where TResultSet : IResultSet;

    public void EmitOne<TSink>(TSink sink, ulong id, AEntity entity, ulong tx) where TSink : IDatomSink;

    public void PopulateAttributeIds(IEnumerable<DbRegisteredAttribute> attributes);
    public ulong TransactAttributeChanges(IDatomStore store, ulong nextTx);
}
