using System;
using System.Linq;
using NexusMods.MneumonicDB.Abstractions;
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
        _stores = new IndexStore[Enum.GetValues(typeof(IndexType)).Length];
        _indexes = new IIndex[Enum.GetValues(typeof(IndexType)).Length];
    }

    public IWriteBatch CreateBatch()
    {
        return new Batch(_stores);
    }

    public void Init(AbsolutePath location) { }

    public void DeclareIndex<TA, TB, TC, TD, TF>(IndexType name)
        where TA : IElementComparer
        where TB : IElementComparer
        where TC : IElementComparer
        where TD : IElementComparer
        where TF : IElementComparer
    {
        var store = new IndexStore(name, _registry);
        _stores[(int)name] = store;
        var index = new Index<TA, TB, TC, TD, TF>(_registry, store);
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
                .Select(i => ((IInMemoryIndex)i).Set).ToArray(),
            _registry);
    }

    public void Dispose() { }
}
