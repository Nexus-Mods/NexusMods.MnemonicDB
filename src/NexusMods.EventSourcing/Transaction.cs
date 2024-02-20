using System.Collections.Concurrent;
using System.Threading;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing;

public class Transaction(Connection connection) : ITransaction
{
    private ulong _tempId = Ids.MinId(Ids.Partition.Tmp) + 1;
    private ConcurrentBag<IDatom> _datoms = new();
    private ConcurrentBag<IReadModel> _models = new();

    /// <inhertdoc />
    public EntityId TempId()
    {
        return EntityId.From(Interlocked.Increment(ref _tempId));
    }

    public void Add(IDatom datom)
    {
        _datoms.Add(datom);
    }

    /// <inheritdoc />
    public TReadModel Add<TReadModel>(TReadModel model)
    where TReadModel : IReadModel
    {
        _models.Add(model);
        return model;
    }

    public void Add<TAttribute, TVal>(EntityId entityId, TVal val, bool isAssert = true) where TAttribute : IAttribute<TVal>
    {
        _datoms.Add(new AssertDatom<TAttribute, TVal>(entityId.Value, val));
    }

    public ICommitResult Commit()
    {
        foreach (var model in _models)
        {
            connection.ModelReflector.Add(this, model);
        }
        return connection.Transact(_datoms);
    }

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(Ids.MinId(Ids.Partition.Tmp));
}
