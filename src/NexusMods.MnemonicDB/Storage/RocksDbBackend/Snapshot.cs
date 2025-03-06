using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal sealed class Snapshot : ADatomsIndex<RocksDbIteratorWrapper>, IRefDatomEnumeratorFactory<RocksDbIteratorWrapper>, ISnapshot
{
    /// <summary>
    /// The backend, needed to create iterators
    /// </summary>
    private readonly Backend _backend;

    /// <summary>
    /// The read options, pre-populated with the snapshot
    /// </summary>
    private readonly ReadOptions _readOptions;
    
    /// <summary>
    /// We keep this here, so that it's not finalized while we're using it
    /// </summary>
    // ReSharper disable once NotAccessedField.Local
    private readonly RocksDbSharp.Snapshot _snapshot;

    public Snapshot(Backend backend, AttributeCache attributeCache, ReadOptions readOptions, RocksDbSharp.Snapshot snapshot) : base(attributeCache)
    {
        _backend = backend;
        _readOptions = readOptions;
        _snapshot = snapshot;
    }

    public IDb MakeDb(TxId txId, AttributeCache attributeCache, IConnection? connection, object? newCache, IndexSegment? recentlyAdded)
    {
        return new Db<Snapshot, RocksDbIteratorWrapper>(this, txId, attributeCache, connection, newCache, recentlyAdded);
    }
    
    public override RocksDbIteratorWrapper GetRefDatomEnumerator() => new(_backend.Db!.NewIterator(null, _readOptions));
}
