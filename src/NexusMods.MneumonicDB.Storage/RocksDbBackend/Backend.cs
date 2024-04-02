using System;
using System.Collections.Generic;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomComparators;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Storage.Abstractions;
using NexusMods.Paths;
using RocksDbSharp;
using IWriteBatch = NexusMods.MneumonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MneumonicDB.Storage.RocksDbBackend;

public class Backend(AttributeRegistry registry) : IStoreBackend
{
    private readonly ColumnFamilies _columnFamilies = new();
    private readonly Dictionary<IndexType, IRocksDbIndex> _indexes = new();
    private readonly Dictionary<IndexType, IndexStore> _stores = new();
    private RocksDb _db = null!;
    private string _location = string.Empty;

    public IWriteBatch CreateBatch()
    {
        return new Batch(_db);
    }

    public void DeclareIndex<TComparator>(IndexType name)
        where TComparator : IDatomComparator<AttributeRegistry>
    {
        var indexStore = new IndexStore(name.ToString(), name, registry);
        _stores.Add(name, indexStore);

        var index = new Index<TComparator>(registry, indexStore);
        _indexes.Add(name, index);
    }

    public IIndex GetIndex(IndexType name)
    {
        return (IIndex)_indexes[name];
    }

    public ISnapshot GetSnapshot()
    {
        return new Snapshot(this, registry);
    }

    public void Init(AbsolutePath location)
    {
        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCreateMissingColumnFamilies()
            .SetCompression(Compression.Lz4);

        foreach (var (name, store) in _stores)
        {
            var index = _indexes[name];
            store.SetupColumnFamily((IIndex)index, _columnFamilies);
        }

        _db = RocksDb.Open(options, location.ToString(), _columnFamilies);

        foreach (var (name, store) in _stores) store.PostOpenSetup(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private class Snapshot(Backend backend, AttributeRegistry registry) : ISnapshot
    {
        private readonly RocksDbSharp.Snapshot _snapshot = backend._db.CreateSnapshot();

        private Iterator GetIteratorInner(IndexType type)
        {
            var options = new ReadOptions()
                .SetSnapshot(_snapshot);

            return backend._db.NewIterator(backend._stores[type].Handle, options);
        }
        public IDatomSource GetIterator(IndexType type, bool historical)
        {
            if (!historical)
            {
                return new IteratorWrapper(GetIteratorInner(type), registry);
            }
            else
            {
                if (!historical) return GetIterator(type, false);

                switch (type)
                {
                    case IndexType.EAVTCurrent:
                    case IndexType.EAVTHistory:
                        return new TemporalIteratorWrapper<EAVTComparator<AttributeRegistry>>(GetIteratorInner(IndexType.EAVTCurrent), GetIteratorInner(IndexType.EAVTHistory), registry);
                    case IndexType.AEVTCurrent:
                    case IndexType.AEVTHistory:
                        return new TemporalIteratorWrapper<AEVTComparator<AttributeRegistry>>(GetIteratorInner(IndexType.AEVTCurrent), GetIteratorInner(IndexType.AEVTHistory), registry);
                    case IndexType.VAETCurrent:
                    case IndexType.VAETHistory:
                        return new TemporalIteratorWrapper<VAETComparator<AttributeRegistry>>(GetIteratorInner(IndexType.VAETCurrent), GetIteratorInner(IndexType.VAETHistory), registry);
                    case IndexType.AVETCurrent:
                    case IndexType.AVETHistory:
                        return new TemporalIteratorWrapper<AVETComparator<AttributeRegistry>>(GetIteratorInner(IndexType.AVETCurrent), GetIteratorInner(IndexType.AVETHistory), registry);
                    case IndexType.TxLog:
                        return new IteratorWrapper(GetIteratorInner(IndexType.TxLog), registry);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown index type");
                }
            }
        }

        public void Dispose()
        {
            _snapshot.Dispose();
        }
    }
}
