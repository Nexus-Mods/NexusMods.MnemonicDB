using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.Paths;
using IWriteBatch = NexusMods.EventSourcing.Storage.Abstractions.IWriteBatch;

namespace NexusMods.EventSourcing.Storage.InMemoryBackend;

public class Backend : IStoreBackend
{
    private readonly IIndex[] _indexes;
    private IndexStore[] _stores;

    private readonly AttributeRegistry _registry;

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

    public void Init(AbsolutePath location)
    {
    }

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

    public void Dispose()
    {
    }
}
