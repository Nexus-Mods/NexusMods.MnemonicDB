using System;
using System.Collections.Generic;
using NexusMods.MneumonicDB.Abstractions;
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

        public IEnumerable<Datom> Datoms(IndexType type, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            var comparator = type.GetComparator(registry);
            var reverse = false;

            var lower = a;
            var upper = b;
            if (comparator.Compare(a, b) > 0)
            {
                reverse = true;
                lower = b;
                upper = a;
            }

            var options = new ReadOptions()
                .SetSnapshot(_snapshot)
                .SetIterateLowerBound(lower.ToArray())
                .SetIterateUpperBound(upper.ToArray());

            return DatomsInner(type, options, reverse);
        }

        private IEnumerable<Datom> DatomsInner(IndexType type, ReadOptions options, bool reverse)
        {
            using var iterator = backend._db.NewIterator(backend._stores[type].Handle, options);
            if (reverse)
                iterator.SeekToLast();
            else
                iterator.SeekToFirst();

            using var writer = new PooledMemoryBufferWriter(128);

            while (iterator.Valid())
            {
                writer.Reset();
                writer.Write(iterator.GetKeySpan());
                yield return new Datom(writer.WrittenMemory, registry);

                if (reverse)
                    iterator.Prev();
                else
                    iterator.Next();
            }
        }
    }
}
