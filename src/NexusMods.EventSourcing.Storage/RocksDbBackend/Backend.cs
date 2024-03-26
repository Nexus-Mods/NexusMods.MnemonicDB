using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.Paths;
using RocksDbSharp;
using IWriteBatch = NexusMods.EventSourcing.Storage.Abstractions.IWriteBatch;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;

public class Backend(AttributeRegistry registry) : IStoreBackend
{
    private string _location = string.Empty;
    private readonly Dictionary<IndexType, IRocksDbIndex> _indexes = new();
    private readonly Dictionary<IndexType, IndexStore> _stores = new();
    private RocksDb _db = null!;
    private readonly ColumnFamilies _columnFamilies = new();

    public IWriteBatch CreateBatch()
    {
        return new Batch(_db);
    }

    public void DeclareIndex<TA, TB, TC, TD, TF>(IndexType name)
        where TA : IElementComparer
        where TB : IElementComparer
        where TC : IElementComparer
        where TD : IElementComparer
        where TF : IElementComparer
    {
        var indexStore = new IndexStore(name.ToString(), name, registry);
        _stores.Add(name, indexStore);

        var index = new Index<TA, TB, TC, TD, TF>(registry, indexStore);
        _indexes.Add(name, index);
    }

    public IIndex GetIndex(IndexType name)
    {
        return (IIndex)_indexes[name];
    }

    private class Snapshot(Backend backend) : ISnapshot
    {
        private readonly RocksDbSharp.Snapshot _snapshot = backend._db.CreateSnapshot();

        public void Dispose()
        {
            _snapshot.Dispose();
        }

        public IDatomIterator GetIterator(IndexType type)
        {
            var options = new ReadOptions()
                .SetSnapshot(_snapshot);

            var iterator = backend._db.NewIterator(backend._stores[type].Handle, options);

            return new IteratorWrapper(iterator);
        }
    }

    public ISnapshot GetSnapshot()
    {
        return new Snapshot(this);
    }

    public void Init(AbsolutePath location)
    {
        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCreateMissingColumnFamilies()
            .SetCompression(Compression.Zstd);

        foreach (var (name, store) in _stores)
        {
            var index = _indexes[name];
            store.SetupColumnFamily((IIndex)index, _columnFamilies);
        }

        _db = RocksDb.Open(options, location.ToString(), _columnFamilies);

        foreach (var (name, store) in _stores)
        {
            store.PostOpenSetup(_db);
        }
    }
}
