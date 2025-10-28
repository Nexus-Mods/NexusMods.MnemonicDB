using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB;

using ResultIterator = HistoryRefDatomEnumerator<TimeFilteredRetractionEnumerator<RocksDbIteratorWrapper>, TimeFilteredEnumerator<RocksDbIteratorWrapper>>;

/// <summary>
/// This is a wrapper around snapshots that allows you to query the snapshot as of a specific transaction
/// id, this requires merging two indexes together, and then the deduplication of the merged index (retractions
/// removing assertions).
/// </summary>
internal class AsOfSnapshot(Snapshot inner, TxId asOfTxId, AttributeResolver attributeResolver) 
    : ADatomsIndex<ResultIterator>(attributeResolver), ISnapshot
{
    public IDb MakeDb(TxId txId, AttributeResolver attributeResolver, IConnection? connection = null)
    {
        return new Db<AsOfSnapshot, ResultIterator>(this, txId, attributeResolver, connection);
    }

    public bool TryGetMaxIdInPartition(PartitionId partitionId, out EntityId id)
    {
        throw new NotSupportedException();
    }

    public ISnapshot AsIf(Datoms datoms)
    {
        throw new NotSupportedException("Cannot currently create an AsIf database on top of an AsOf database");
    }

    public override ResultIterator GetRefDatomEnumerator()
    {
        return new ResultIterator(
            new TimeFilteredRetractionEnumerator<RocksDbIteratorWrapper>(inner.GetRefDatomEnumerator(), asOfTxId),
            new TimeFilteredEnumerator<RocksDbIteratorWrapper>(inner.GetRefDatomEnumerator(), asOfTxId));
    }
}
