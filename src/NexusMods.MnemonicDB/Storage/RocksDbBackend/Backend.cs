using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.Paths;
using RocksDbSharp;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class Backend(AttributeCache attributeCache) : IStoreBackend
{
    private readonly ColumnFamilies _columnFamilies = new();
    private readonly Dictionary<IndexType, IRocksDbIndex> _indexes = new();
    internal readonly Dictionary<IndexType, IRocksDBIndexStore> Stores = new();
    internal RocksDb? Db = null!;

    public IWriteBatch CreateBatch()
    {
        return new Batch(Db!);
    }

    public void DeclareIndex<TComparator>(IndexType name)
        where TComparator : IDatomComparator
    {
        var indexStore = new IndexStore<TComparator>(name.ToString(), name);
        Stores.Add(name, indexStore);

        var index = new Index<TComparator>(indexStore);
        _indexes.Add(name, index);
    }

    public IIndex GetIndex(IndexType name)
    {
        return (IIndex)_indexes[name];
    }

    public ISnapshot GetSnapshot()
    {
        return new Snapshot(this, attributeCache);
    }

    public void Init(AbsolutePath location)
    {
        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCreateMissingColumnFamilies()
            .SetCompression(Compression.Lz4);

        foreach (var (name, store) in Stores)
        {
            var index = _indexes[name];
            store.SetupColumnFamily((IIndex)index, _columnFamilies);
        }

        Db = RocksDb.Open(options, location.ToString(), _columnFamilies);

        foreach (var (name, store) in Stores) store.PostOpenSetup(Db);
    }

    public void Dispose()
    {
        Db?.Dispose();
    }
}
