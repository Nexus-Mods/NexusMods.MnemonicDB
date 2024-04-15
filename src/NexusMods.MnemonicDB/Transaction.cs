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
    private bool _committed = false;

    /// <inhertdoc />
    public EntityId TempId()
    {
        return EntityId.From(Interlocked.Increment(ref _tempId));
    }

    public void Add<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val)
    {
        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed");
        _datoms.Add(entityId, attribute, val, ThisTxId, false);
    }

    public void Retract<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val)
    {
        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed");
        _datoms.Add(entityId, attribute, val, ThisTxId, true);
    }

    public async Task<ICommitResult> Commit()
    {
        _committed = true;
        return await connection.Transact(_datoms.Build());
    }

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(Ids.MinId(Ids.Partition.Tmp));

    public void Dispose()
    {
        _datoms.Dispose();
    }
}
