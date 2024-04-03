using System;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Storage.Abstractions;
using NexusMods.Paths;
using IWriteBatch = NexusMods.MneumonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MneumonicDB.Storage.InMemoryBackend;

public class Backend : IStoreBackend
{
    private readonly IIndex[] _indexes;

    private readonly AttributeRegistry _registry;
    private readonly IndexStore[] _stores;

    public Backend(AttributeRegistry registry)
    {
        _registry = registry;
        _stores = new IndexStore[Enum.GetValues<IndexType>().Select(i => (int)i).Max() + 1];
        _indexes = new IIndex[Enum.GetValues<IndexType>().Select(i => (int)i).Max() + 1];
    }

    public IWriteBatch CreateBatch()
    {
        return new Batch(_stores);
    }

    public void Init(AbsolutePath location) { }

    public void DeclareIndex<TComparator>(IndexType name)
        where TComparator : IDatomComparator<AttributeRegistry>
    {
        var store = new IndexStore(name, _registry);
        _stores[(int)name] = store;
        var index = new Index<TComparator>(_registry, store);
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
            _registry);
    }

    public void Dispose() { }
}
