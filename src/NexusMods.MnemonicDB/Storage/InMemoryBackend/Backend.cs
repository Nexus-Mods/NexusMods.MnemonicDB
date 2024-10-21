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

public class Backend : IStoreBackend
{
    private IndexData _index;
    private readonly AttributeCache _attributeCache;

    public Backend()
    {
        _attributeCache = new AttributeCache();
        _index = IndexData.Empty.WithComparer(new GlobalComparer());
    }

    public AttributeCache AttributeCache => _attributeCache;

    public IWriteBatch CreateBatch()
    {
        return new Batch(this);
    }
    
    internal void Alter(Func<IndexData, IndexData> alter)
    {
        _index = alter(_index);
    }

    public void Init(AbsolutePath location) { }
    
    public ISnapshot GetSnapshot()
    {
        return new Snapshot(_index, AttributeCache);
    }

    public void Dispose() { }
}
