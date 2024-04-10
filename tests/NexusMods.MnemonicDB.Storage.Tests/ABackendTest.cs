using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;


namespace NexusMods.MnemonicDB.Storage.Tests;

public abstract class ABackendTest<TStoreType>(
    IServiceProvider provider,
    Func<AttributeRegistry, IStoreBackend> backendFn)
    : AStorageTest(provider, backendFn)
    where TStoreType : IStoreBackend
{
    [Theory]
    [InlineData(IndexType.TxLog)]
    [InlineData(IndexType.EAVTHistory)]
    [InlineData(IndexType.EAVTCurrent)]
    [InlineData(IndexType.AEVTCurrent)]
    [InlineData(IndexType.AEVTHistory)]
    [InlineData(IndexType.VAETCurrent)]
    [InlineData(IndexType.VAETHistory)]
    [InlineData(IndexType.AVETCurrent)]
    [InlineData(IndexType.AVETHistory)]
    public async Task InsertedDatomsShowUpInTheIndex(IndexType type)
    {
        var tx = await GenerateData();
        var datoms = tx.Snapshot
            .Datoms(type)
            .Select(d => d.Resolved)
            .ToArray();

        await Verify(datoms.ToTable(Registry))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }

    [Theory]
    [InlineData(IndexType.EAVTHistory)]
    [InlineData(IndexType.EAVTCurrent)]
    [InlineData(IndexType.AEVTCurrent)]
    [InlineData(IndexType.AEVTHistory)]
    [InlineData(IndexType.VAETCurrent)]
    [InlineData(IndexType.VAETHistory)]
    [InlineData(IndexType.AVETCurrent)]
    [InlineData(IndexType.AVETHistory)]
    public async Task HistoricalQueriesReturnAllDataSorted(IndexType type)
    {
        var tx = await GenerateData();
        var current = tx.Snapshot.Datoms(type.CurrentVariant());
        var history = tx.Snapshot.Datoms(type.HistoryVariant());
        var comparer = type.GetComparator(Registry);
        var merged = current
            .Merge(history, (a, b) => comparer.Compare(a.RawSpan, b.RawSpan))
            .Select(d => d.Resolved)
            .ToArray();

        await Verify(merged.ToTable(Registry))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }


    private async Task<StoreResult> GenerateData()
    {
        var id1 = NextTempId();
        var id2 = NextTempId();

        var modId1 = NextTempId();
        var modId2 = NextTempId();
        var loadoutId = NextTempId();
        var collectionId = NextTempId();


        StoreResult tx;

        {

            using var segment = new IndexSegmentBuilder(Registry);

            segment.Add(id1, File.Path, "/foo/bar");
            segment.Add(id1, File.Hash, Hash.From(0xDEADBEEF));
            segment.Add(id1, File.Size, Size.From(42));
            segment.Add(id2, File.Path, "/qix/bar");
            segment.Add(id2, File.Hash, Hash.From(0xDEADBEAF));
            segment.Add(id2, File.Size, Size.From(77));
            segment.Add(id1, File.ModId, modId1);
            segment.Add(id2, File.ModId, modId1);
            segment.Add(modId1, Mod.Name, "Test Mod 1");
            segment.Add(modId1, Mod.LoadoutId, loadoutId);
            segment.Add(modId2, Mod.Name, "Test Mod 2");
            segment.Add(modId2, Mod.LoadoutId, loadoutId);
            segment.Add(loadoutId, Loadout.Name, "Test Loadout 1");
            segment.Add(collectionId, Collection.Name, "Test Collection 1");
            segment.Add(collectionId, Collection.Loadout, loadoutId);
            segment.Add(collectionId, Collection.Mods, modId1);
            segment.Add(collectionId, Collection.Mods, modId2);

            tx = await DatomStore.Transact(segment.Build());
        }

        id1 = tx.Remaps[id1];
        id2 = tx.Remaps[id2];
        modId2 = tx.Remaps[modId2];
        collectionId = tx.Remaps[collectionId];

        {
            using var segment = new IndexSegmentBuilder(Registry);
            segment.Add(id2, File.Path, "/foo/qux");
            segment.Add(id1, File.ModId, modId2);
            segment.Add(collectionId, Collection.Mods, modId2, true);
            tx = await DatomStore.Transact(segment.Build());
        }
        return tx;
    }

    [Theory]
    [InlineData(IndexType.TxLog)]
    [InlineData(IndexType.EAVTHistory)]
    [InlineData(IndexType.EAVTCurrent)]
    [InlineData(IndexType.AEVTCurrent)]
    [InlineData(IndexType.AEVTHistory)]
    [InlineData(IndexType.VAETCurrent)]
    [InlineData(IndexType.VAETHistory)]
    [InlineData(IndexType.AVETCurrent)]
    [InlineData(IndexType.AVETHistory)]
    public async Task RetractedValuesAreSupported(IndexType type)
    {
        var id = NextTempId();
        var modId = NextTempId();

        StoreResult tx1, tx2;

        {
            using var segment = new IndexSegmentBuilder(Registry);

            segment.Add(id, File.Path, "/foo/bar");
            segment.Add(id, File.Hash, Hash.From(0xDEADBEEF));
            segment.Add(id, File.Size, Size.From(42));
            segment.Add(id, File.ModId, modId);

            tx1 = await DatomStore.Transact(segment.Build());
        }

        id = tx1.Remaps[id];
        modId = tx1.Remaps[modId];

        {
            using var segment = new IndexSegmentBuilder(Registry);

            segment.Add(id, File.Path, "/foo/bar", true);
            segment.Add(id, File.Hash, Hash.From(0xDEADBEEF), true);
            segment.Add(id, File.Size, Size.From(42), true);
            segment.Add(id, File.ModId, modId, true);

            tx2 = await DatomStore.Transact(segment.Build());

        }


        var datoms = tx2.Snapshot
            .Datoms(type)
            .Select(d => d.Resolved)
            .ToArray();
        await Verify(datoms.ToTable(Registry))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }
}
