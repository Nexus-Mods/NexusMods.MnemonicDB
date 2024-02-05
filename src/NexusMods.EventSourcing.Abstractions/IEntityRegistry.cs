using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions.BuiltinEntities;

namespace NexusMods.EventSourcing.Abstractions;

public interface IEntityRegistry
{
    public delegate void EmitEntity<TSink>(AEntity input, ulong e, ulong t, ref TSink sink) where TSink : IDatomSink;

    public EmitEntity<TSink> MakeEmitter<TSink>(Type entityType) where TSink : IDatomSink;

    public void PopulateAttributeIds(IEnumerable<DbRegisteredAttribute> attributes);
    public ulong TransactAttributeChanges(IDatomStore store, ulong nextTx);
}
