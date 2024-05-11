using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB;

/// <inheritdoc />
internal class Transaction(Connection connection, IAttributeRegistry registry) : ITransaction
{
    private readonly IndexSegmentBuilder _datoms = new(registry);
    private HashSet<ITxFunction>? _txFunctions = null; // No reason to create the hashset if we don't need it
    private List<TempEntity>? _tempEntities = null;
    private ulong _tempId = Ids.MinId(Ids.Partition.Tmp) + 1;
    private bool _committed = false;

    /// <inhertdoc />
    public EntityId TempId(byte entityPartition = (byte)Ids.Partition.Entity)
    {
        var tempId = Interlocked.Increment(ref _tempId);
        // Add the partition to the id
        var actualId = ((ulong)entityPartition << 40) | tempId;
        return EntityId.From(actualId);
    }

    public void Add<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val, bool isRetract = false)
    {
        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed");
        _datoms.Add(entityId, attribute, val, ThisTxId, isRetract);
    }

    public void Add(ITxFunction fn)
    {
        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed");
        _txFunctions ??= [];
        _txFunctions?.Add(fn);
    }

    public void Attach(TempEntity entity)
    {
        _tempEntities ??= [];
        _tempEntities.Add(entity);
    }

    public async Task<ICommitResult> Commit()
    {
        _committed = true;
        if (_tempEntities != null)
        {
            foreach (var entity in _tempEntities!)
            {
                entity.AddTo(this);
            }
        }
        return await connection.Transact(_datoms.Build(), _txFunctions);
    }

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(Ids.MinId(Ids.Partition.Tmp));

    public void Dispose()
    {
        _datoms.Dispose();
    }
}
