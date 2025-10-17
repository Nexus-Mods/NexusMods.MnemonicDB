using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
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
    
    internal RocksDbSharp.Snapshot NativeSnapshot => _snapshot;

    public IDb MakeDb(TxId txId, AttributeCache attributeCache, IConnection? connection)
    {
        return new Db<Snapshot, RocksDbIteratorWrapper>(this, txId, attributeCache, connection);
    }

    public unsafe bool TryGetMaxIdInPartition(PartitionId partitionId, out EntityId id)
    {
        var prefix = new KeyPrefix(partitionId.MaxValue, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, IndexType.EAVTCurrent);
        var spanTo = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in prefix, 1));
        
        var options = new ReadOptions()
            .SetTotalOrderSeek(true)
            .SetSnapshot(_snapshot)
            .SetPinData(false);
        using var it = _backend.Db!.NewIterator(null, options);
        it.Seek(spanTo);
        it.Prev();

        if (!it.Valid())
        {
            id = partitionId.MinValue;
            return false;
        }

        var keySpan = it.GetKeySpan();
        var prefixRead = KeyPrefix.Read(keySpan);
        id = prefixRead.E;
        if (id.Partition == partitionId)
            return true;
        id = partitionId.MinValue;
        return false;
    }

    public override RocksDbIteratorWrapper GetRefDatomEnumerator(bool totalOrdered)
    {
        if (!totalOrdered) 
            return new RocksDbIteratorWrapper(_backend.Db!.NewIterator(null, _readOptions));
        var options = new ReadOptions()
            .SetTotalOrderSeek(true)
            .SetSnapshot(_snapshot)
            .SetPinData(true);
        return new RocksDbIteratorWrapper(_backend.Db!.NewIterator(null, options));
    }
}
