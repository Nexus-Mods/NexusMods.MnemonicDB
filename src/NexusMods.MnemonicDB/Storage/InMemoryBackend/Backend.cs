using System;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.Paths;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

using IndexData = ImmutableSortedSet<byte[]>;

/// <summary>
/// The in-memory backend for the datoms store
/// </summary>
public class Backend : IStoreBackend
{
    private IndexData _index;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Backend()
    {
        AttributeCache = new AttributeCache();
        _index = IndexData.Empty.WithComparer(new GlobalComparer());
    }

    /// <inheritdoc />
    public AttributeCache AttributeCache { get; }

    /// <inheritdoc />
    public IWriteBatch CreateBatch()
    {
        return new Batch(this);
    }
    
    internal void Alter(Func<IndexData, IndexData> alter)
    {
        _index = alter(_index);
    }

    /// <inheritdoc />
    public void Init(AbsolutePath location) { }

    /// <inheritdoc />
    public ISnapshot GetSnapshot()
    {
        return new Snapshot(_index, AttributeCache);
    }

    /// <inheritdoc />
    public void FlushAndCompact()
    {
        // No need to do anything
    }

    /// <inheritdoc />
    public void Dispose() { }
}
