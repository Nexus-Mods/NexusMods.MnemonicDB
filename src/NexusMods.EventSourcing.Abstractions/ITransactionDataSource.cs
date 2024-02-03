using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface ITransactionDataSource
{
    void Emit<TSink>(IDatomSink sink);
    void RemapTempId<TSing>(EntityId tempId, EntityId permId);
}
