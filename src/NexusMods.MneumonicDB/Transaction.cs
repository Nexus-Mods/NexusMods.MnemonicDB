using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;

namespace NexusMods.MneumonicDB;

/// <inheritdoc />
internal class Transaction(Connection connection) : ITransaction
{
    private readonly ConcurrentBag<IWriteDatom> _datoms = new();
    private readonly ConcurrentBag<IReadModel> _models = new();
    private ulong _tempId = Ids.MinId(Ids.Partition.Tmp) + 1;

    /// <inhertdoc />
    public EntityId TempId()
    {
        return EntityId.From(Interlocked.Increment(ref _tempId));
    }

    /// <inheritdoc />
    public void Add<TReadModel>(TReadModel model)
        where TReadModel : IReadModel
    {
        _models.Add(model);
    }

    public void Add<TAttribute, TVal>(EntityId entityId, TVal val) where TAttribute : IAttribute<TVal>
    {
        _datoms.Add(TAttribute.Assert(entityId, val));
    }

    public async Task<ICommitResult> Commit()
    {
        foreach (var model in _models) connection.ModelReflector.Add(this, model);
        return await connection.Transact(_datoms);
    }

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(Ids.MinId(Ids.Partition.Tmp));
}
