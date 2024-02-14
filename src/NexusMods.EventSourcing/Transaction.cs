using System;
using System.Collections.Concurrent;
using System.Threading;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class Transaction(Connection connection) : ITransaction
{
    private ulong _tempId = Ids.MinId(Ids.Partition.Tmp);
    private ConcurrentBag<IDatom> _datoms = new();

    /// <inhertdoc />
    public EntityId TempId()
    {
        return EntityId.From(Interlocked.Increment(ref _tempId));
    }

    public void Add(IDatom datom)
    {
        _datoms.Add(datom);
    }

    public ICommitResult Commit()
    {
        return connection.Transact(_datoms);
    }
}
