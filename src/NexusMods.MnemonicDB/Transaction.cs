using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

/// <inheritdoc />
internal class Transaction(Connection connection, IAttributeRegistry registry) : ITransaction, IDisposable
{
    private readonly IndexSegmentBuilder _datoms = new(registry);
    private ulong _tempId = Ids.MinId(Ids.Partition.Tmp) + 1;

    /// <inhertdoc />
    public EntityId TempId()
    {
        return EntityId.From(Interlocked.Increment(ref _tempId));
    }

    public void Add<TVal>(EntityId entityId, Attribute<TVal> attribute, TVal val)
    {
        _datoms.Add(entityId, attribute, val, ThisTxId, false);
    }

    public void Retract<TVal>(EntityId entityId, Attribute<TVal> attribute, TVal val)
    {
        _datoms.Add(entityId, attribute, val, ThisTxId, true);
    }

    public async Task<ICommitResult> Commit()
    {
        return await connection.Transact(_datoms.Build());
    }

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(Ids.MinId(Ids.Partition.Tmp));

    public void Dispose()
    {
        _datoms.Dispose();
    }
}
