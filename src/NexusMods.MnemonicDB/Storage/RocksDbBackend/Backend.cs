using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.Paths;
using RocksDbSharp;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class Backend : IStoreBackend
{
    internal RocksDb? Db = null!;
    private IntPtr _comparator;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Backend()
    {
        AttributeCache = new AttributeCache();
    }

    /// <inheritdoc />
    public AttributeCache AttributeCache { get; }

    /// <inheritdoc />
    public IWriteBatch CreateBatch()
    {
        return new Batch(Db!);
    }

    /// <inheritdoc />
    public ISnapshot GetSnapshot()
    {
        return new Snapshot(this, AttributeCache);
    }

    /// <inheritdoc />
    public void Init(AbsolutePath location)
    {

        _comparator = Native.Instance.rocksdb_comparator_create(IntPtr.Zero, 
            NativeComparators.GetDestructorPtr(),
            NativeComparators.GetNativeFnPtr(),
            NativeComparators.GetNamePtr());

        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCreateMissingColumnFamilies()
            .SetCompression(Compression.Lz4)
            .SetComparator(_comparator);
        
        Db = RocksDb.Open(options, location.ToString());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Db?.Dispose();
    }
}
