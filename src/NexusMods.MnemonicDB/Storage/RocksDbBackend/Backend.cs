using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.Paths;
using RocksDbSharp;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

/// <summary>
/// Backend for the RocksDB storage engine.
/// </summary>
public class Backend : IStoreBackend
{
    internal RocksDb? Db = null!;
    private IntPtr _comparator;
    private readonly bool _isReadOnly;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Backend(bool readOnly = false)
    {
        _isReadOnly = readOnly;
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
            .SetCompression(Compression.Zstd)
            .SetComparator(_comparator);
        
        Native.Instance.rocksdb_options_set_bottommost_compression(options.Handle, (int)Compression.Zstd);
        
        if (_isReadOnly)
            Db = RocksDb.OpenReadOnly(options, location.ToString(), false);
        else 
            Db = RocksDb.Open(options, location.ToString());
    }

    /// <summary>
    /// Flushes all the logs to disk, and performs a compaction, recommended if you want to archive the database
    /// and move it somewhere else.
    /// </summary>
    public void FlushAndCompact()
    {
        Db?.Flush(new FlushOptions().SetWaitForFlush(true));
        Db?.CompactRange([0x00], [0xFF]);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Db?.Dispose();
    }
}
