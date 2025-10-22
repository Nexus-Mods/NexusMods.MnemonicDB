using System.Collections.Generic;
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
    internal readonly Backend Backend;

    /// <summary>
    /// The read options, pre-populated with the snapshot
    /// </summary>
    internal readonly ReadOptions ReadOptions;
    
    /// <summary>
    /// We keep this here, so that it's not finalized while we're using it
    /// </summary>
    // ReSharper disable once NotAccessedField.Local
    internal readonly RocksDbSharp.Snapshot NativeSnapshot;

    public Snapshot(Backend backend, AttributeCache attributeCache, ReadOptions readOptions, RocksDbSharp.Snapshot nativeSnapshot) : base(attributeCache)
    {
        Backend = backend;
        ReadOptions = readOptions;
        NativeSnapshot = nativeSnapshot;
    }
    
    public IDb MakeDb(TxId txId, AttributeCache attributeCache, IConnection? connection)
    {
        return new Db<Snapshot, RocksDbIteratorWrapper>(this, txId, attributeCache, connection);
    }

    public bool TryGetMaxIdInPartition(PartitionId partitionId, out EntityId id)
    {
        var prefix = new KeyPrefix(partitionId.MaxValue, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, IndexType.EAVTCurrent);
        var spanTo = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in prefix, 1));
        
        var options = new ReadOptions()
            .SetTotalOrderSeek(true)
            .SetSnapshot(NativeSnapshot)
            .SetPinData(false);
        using var it = Backend.Db!.NewIterator(null, options);
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

    public ISnapshot AsIf(Datoms datoms)
    {
        var (retracts, asserts) = TxProcessing.NormalizeWithTxIds(CollectionsMarshal.AsSpan(datoms), this, KeyPrefix.MaxPossibleTxId);
        var batch = new WriteBatchWithIndex(this, AttributeCache);
        
        foreach (var retract in retracts)
            TxProcessing.LogRetract(batch, retract, KeyPrefix.MaxPossibleTxId, AttributeCache);

        foreach (var assert in asserts)
        {
            TxProcessing.LogAssert(batch, assert, AttributeCache);
        }

        return batch;


    }

    public override RocksDbIteratorWrapper GetRefDatomEnumerator()
    {
        return new RocksDbIteratorWrapper(Backend.Db!, NativeSnapshot, ReadOptions);;
    }
}
