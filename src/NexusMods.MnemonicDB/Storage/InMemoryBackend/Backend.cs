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

public class Backend : IStoreBackend
{
    private readonly IIndex[] _indexes;

    private readonly AttributeCache _attributeCache;
    private readonly IndexStore[] _stores;

    public Backend(AttributeCache attributeCache)
    {
        _attributeCache = attributeCache;
        _stores = new IndexStore[Enum.GetValues<IndexType>().Select(i => (int)i).Max() + 1];
        _indexes = new IIndex[Enum.GetValues<IndexType>().Select(i => (int)i).Max() + 1];
    }

    public IWriteBatch CreateBatch()
    {
        return new Batch(_stores);
    }

    public void Init(AbsolutePath location) { }

    public void DeclareIndex<TComparator>(IndexType name)
        where TComparator : IDatomComparator
    {
        var store = new IndexStore(name);
        _stores[(int)name] = store;
        var index = new Index<TComparator>(store);
        store.Init(index);
        _indexes[(int)name] = index;
    }

    public IIndex GetIndex(IndexType name)
    {
        return _indexes[(int)name];
    }

    public ISnapshot GetSnapshot()
    {
        return new Snapshot(_indexes
                .Select(i => i == null ? ImmutableSortedSet<byte[]>.Empty : ((IInMemoryIndex)i).Set).ToArray(),
            _attributeCache);
    }

    public void Dispose() { }
}
