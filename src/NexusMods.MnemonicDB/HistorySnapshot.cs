using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB;


using ResultIterator = HistoryRefDatomEnumerator<RocksDbIteratorWrapper, RocksDbIteratorWrapper>;

/// <summary>
/// This is a wrapper around snapshots that allows you to query the snapshot as of a specific transaction
/// id, this requires merging two indexes together, and then the deduplication of the merged index (retractions
/// removing assertions).
/// </summary>
internal class HistorySnapshot(Snapshot inner, AttributeCache attributeCache) : ADatomsIndex<ResultIterator>(attributeCache), IRefDatomEnumeratorFactory<ResultIterator>, ISnapshot
{
    public IDb MakeDb(TxId txId, AttributeCache cache, IConnection? connection = null, object? newCache = null, IndexSegment? recentlyAdded = null)
    {
        return new Db<HistorySnapshot, ResultIterator>(this, txId, cache, connection, newCache, recentlyAdded);
    }

    public override ResultIterator GetRefDatomEnumerator()
    {
        return new ResultIterator(inner.GetRefDatomEnumerator(), inner.GetRefDatomEnumerator());
    }
}
