using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
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
    private readonly bool _isReadOnly;
    private Env? _env;
    private IntPtr _sliceTransform;
    private volatile bool _disposed;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Backend(bool readOnly = false)
    {
        _disposed = false;
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
        var snapShot = Db!.CreateSnapshot();
        var readOptions = new ReadOptions()
            .SetTotalOrderSeek(false)
            .SetSnapshot(snapShot)
            .SetPinData(true);
        var snapshot = new Snapshot(this, AttributeCache, readOptions, snapShot);
        return snapshot;
    }

    /// <inheritdoc />
    public void Init(DatomStoreSettings settings)
    {
        _sliceTransform = NativePrefixExtractor.MakeSliceTransform();
        
        if (settings.IsInMemory)
            _env = Env.CreateMemEnv();
        else
            _env = Env.CreateDefaultEnv();
        
        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCreateMissingColumnFamilies()
            .SetCompression(Compression.Zstd)
            .SetComparator(NativeComparators.ComparatorPtr)
            .SetPrefixExtractor(_sliceTransform)
            .SetEnv(_env.Handle);

        var tableOptions = new BlockBasedTableOptions()
            .SetFilterPolicy(BloomFilterPolicy.Create(10,true));

        options.SetBlockBasedTableFactory(tableOptions);
        
        Native.Instance.rocksdb_options_set_bottommost_compression(options.Handle, (int)Compression.Zstd);

        var nativePath = settings.Path?.ToString() ?? "/dev/in-memory";
        
        if (_isReadOnly)
            Db = RocksDb.OpenReadOnly(options, nativePath, false);
        else 
            Db = RocksDb.Open(options, nativePath);
    }

    /// <summary>
    /// Flushes all the logs to disk, and performs a compaction, recommended if you want to archive the database
    /// and move it somewhere else.
    /// </summary>
    public void FlushAndCompact(bool verify)
    {
        if (verify)
            VerifyOrdering();
        Db?.Flush(new FlushOptions().SetWaitForFlush(true));
        Native.Instance.rocksdb_compact_range(Db!.Handle, IntPtr.Zero, 0, IntPtr.Zero, 0);
        Db?.Flush(new FlushOptions().SetWaitForFlush(true));
    }

    private unsafe void VerifyOrdering()
    {
        using var iterator = Db!.NewIterator();
        iterator.SeekToFirst();

        var prevPtrCache = new PtrCache();
        while (iterator.Valid())
        {
            var ptr = Native.Instance.rocksdb_iter_key(iterator.Handle, out var len);
            var thisPtr = new Ptr((byte*)ptr,(int)len);
            if (!prevPtrCache.IsNull)
            {
                var cmp= GlobalComparer.Compare(thisPtr.Base, thisPtr.Length, prevPtrCache.Ptr.Base, prevPtrCache.Ptr.Length);
                if (cmp <= 0)
                {
                    var prevPrefix = prevPtrCache.Ptr.Read<KeyPrefix>(0);
                    var thisPrefix = thisPtr.Read<KeyPrefix>(0);

                    var sb = new StringBuilder();
                    sb.Append($"Validation failed, ordering is not correct - result: {cmp}");
                    sb.AppendLine($"    this: {thisPrefix} - index: {thisPrefix.Index} - type: {thisPrefix.ValueTag} - length: {thisPtr.Length}");
                    sb.AppendLine($"    prev: {prevPrefix} - index: {prevPrefix.Index} - type: {prevPrefix.ValueTag} - length: {prevPtrCache.Ptr.Length}");
                    
                    throw new Exception(sb.ToString());
                }
            }
            prevPtrCache.CopyFrom(thisPtr);
            iterator.Next();
        } 
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Db?.Dispose();
    }
}
