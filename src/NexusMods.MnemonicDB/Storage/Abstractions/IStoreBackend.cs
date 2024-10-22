using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

public interface IStoreBackend : IDisposable
{
    /// <summary>
    /// Returns the attribute cache for this store, this cache should be shared across
    /// the datom store, the connection, and the db instances
    /// </summary>
    public AttributeCache AttributeCache { get; }
    public IWriteBatch CreateBatch();

    public void Init(AbsolutePath location);

    /// <summary>
    ///     Gets a snapshot of the current state of the store that will not change
    ///     during calls to GetIterator
    /// </summary>
    public ISnapshot GetSnapshot();
    
}
