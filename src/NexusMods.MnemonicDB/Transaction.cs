using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB;

/// <inheritdoc />
internal class Transaction(Connection connection) : ITransaction
{
    private readonly ConcurrentBag<IWriteDatom> _datoms = new();
    private ulong _tempId = Ids.MinId(Ids.Partition.Tmp) + 1;

    /// <inhertdoc />
    public EntityId TempId()
    {
        return EntityId.From(Interlocked.Increment(ref _tempId));
    }

    public void Add<TAttribute, TVal>(EntityId entityId, TVal val) where TAttribute : IAttribute<TVal>
    {
        _datoms.Add(TAttribute.Assert(entityId, val));
    }

    public async Task<ICommitResult> Commit()
    {
        return await connection.Transact(_datoms);
    }

    public ModelHeader New()
    {
        return new ModelHeader
        {
            Id = TempId(),
            Tx = this
        };
    }

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(Ids.MinId(Ids.Partition.Tmp));
}
