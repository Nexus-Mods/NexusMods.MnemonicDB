using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal sealed class Snapshot : ADatomsIndex<RocksDbIteratorWrapper>, ISnapshot<RocksDbIteratorWrapper>
{
    /// <summary>
    /// The backend, needed to create iterators
    /// </summary>
    private readonly Backend _backend;

    /// <summary>
    /// The read options, pre-populated with the snapshot
    /// </summary>
    private readonly ReadOptions _readOptions;

    private readonly AttributeCache _attributeCache;
    
    /// <summary>
    /// We keep this here, so that it's not finalized while we're using it
    /// </summary>
    // ReSharper disable once NotAccessedField.Local
    private readonly RocksDbSharp.Snapshot _snapshot;

    public Snapshot(Backend backend, AttributeCache attributeCache, ReadOptions readOptions, RocksDbSharp.Snapshot snapshot) : base(attributeCache)
    {
        _backend = backend;
        _attributeCache = attributeCache;
        _readOptions = readOptions;
        _snapshot = snapshot;
    }

    RocksDbIteratorWrapper ISnapshot<RocksDbIteratorWrapper>.GetLowLevelIterator()
    {
        return GetLowLevelIterator();
    }

    public IDb MakeDb(TxId txId, AttributeCache attributeCache, IConnection? connection, object? newCache, IndexSegment? recentlyAdded)
    {
        return new Db<Snapshot, RocksDbIteratorWrapper>(this, txId, attributeCache, connection, newCache, recentlyAdded);
    }
    


    protected override RocksDbIteratorWrapper GetLowLevelIterator() => new(_backend.Db!.NewIterator(null, _readOptions));
}
