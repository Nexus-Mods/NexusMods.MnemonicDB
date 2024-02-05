using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class Connection : IConnection
{
    private readonly DuckDBDatomStore _store;
    private readonly ulong _nextTx;
    private readonly IEntityRegistry _registry;

    public Connection(DuckDBDatomStore store, IEntityRegistry registry)
    {
        _registry = registry;
        _store = store;
        _nextTx = Ids.MinId(IdSpace.Tx) + 10;
    }

    public TransactionId Commit(Transaction transaction)
    {
        throw new NotImplementedException();

    }
}
