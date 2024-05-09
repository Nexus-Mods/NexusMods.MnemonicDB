using System;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB.Storage;

internal class InternalTransaction(IndexSegmentBuilder datoms) : ITransaction
{
    private ulong _tempId = Ids.MinId(Ids.Partition.Tmp) + 1;

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(Ids.MinId(Ids.Partition.Tmp));


    /// <inhertdoc />
    public EntityId TempId(byte entityPartition = (byte)Ids.Partition.Entity)
    {
        var tempId = Interlocked.Increment(ref _tempId);
        // Add the partition to the id
        var actualId = ((ulong)entityPartition << 40) | tempId;
        return EntityId.From(actualId);
    }

    /// <inheritdoc />
    public void Add<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val, bool isRetract = false)
    {
        datoms.Add(entityId, attribute, val, ThisTxId, isRetract);
    }

    /// <inheritdoc />
    public void Add(ITxFunction fn)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<ICommitResult> Commit()
    {
        throw new NotSupportedException();
    }

    public TModel New<TModel>() where TModel : IModel
    {
        throw new NotImplementedException();
    }

    public TModel Edit<TModel>(TModel model) where TModel : IModel
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }


}
