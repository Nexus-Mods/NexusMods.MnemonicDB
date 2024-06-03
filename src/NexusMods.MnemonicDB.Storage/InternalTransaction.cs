﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB.Storage;

internal class InternalTransaction(IndexSegmentBuilder datoms) : ITransaction
{
    private ulong _tempId = PartitionId.Temp.MakeEntityId(0).Value;

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(PartitionId.Temp.MakeEntityId(0).Value);

    /// <inheritdoc />
    public EntityId TempId()
    {
        return TempId(PartitionId.Entity);
    }


    /// <inhertdoc />
    public EntityId TempId(PartitionId partition)
    {
        var tempId = Interlocked.Increment(ref _tempId);
        // Add the partition to the id
        var actualId = ((ulong)partition.Value << 40) | tempId;
        return EntityId.From(actualId);
    }

    /// <inheritdoc />
    public void Add<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val, bool isRetract = false)
    {
        datoms.Add(entityId, attribute, val, ThisTxId, isRetract);
    }

    public void Add(EntityId entityId, ReferencesAttribute attribute, IEnumerable<EntityId> ids)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public void Add(ITxFunction fn)
    {
        throw new NotSupportedException();
    }
    public void Attach(ITemporaryEntity entity)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<ICommitResult> Commit()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }


}
