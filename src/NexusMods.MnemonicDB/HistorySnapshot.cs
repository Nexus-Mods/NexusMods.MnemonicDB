using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB;


using ResultIterator = HistoryMergeIterator<RocksDbIteratorWrapper, RocksDbIteratorWrapper>;

/// <summary>
/// This is a wrapper around snapshots that allows you to query the snapshot as of a specific transaction
/// id, this requires merging two indexes together, and then the deduplication of the merged index (retractions
/// removing assertions).
/// </summary>
internal class HistorySnapshot(Snapshot inner, AttributeCache attributeCache) : ADatomsIndex<ResultIterator>(attributeCache), ILowLevelIteratorFactory<ResultIterator>
{
    public IDb MakeDb(TxId txId, AttributeCache cache, IConnection? connection = null, object? newCache = null, IndexSegment? recentlyAdded = null)
    {
        throw new NotImplementedException();
        //return new Db<HistorySnapshot, ResultIterator>(this, txId, cache, connection, newCache, recentlyAdded);
    }

    public override ResultIterator GetLowLevelIterator()
    {
        return new ResultIterator(inner.GetLowLevelIterator(), inner.GetLowLevelIterator());
    }

    ResultIterator ILowLevelIteratorFactory<ResultIterator>.GetLowLevelIterator()
    {
        return GetLowLevelIterator();
    }
}
