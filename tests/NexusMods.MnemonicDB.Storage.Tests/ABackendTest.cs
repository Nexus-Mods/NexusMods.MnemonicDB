using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
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

        var tx = await DatomStore.Transact([
            File.Path.Assert(id1, "/foo/bar"),
            File.Hash.Assert(id1, Hash.From(0xDEADBEEF)),
            File.Size.Assert(id1, Size.From(42)),
            File.Path.Assert(id2, "/qix/bar"),
            File.Hash.Assert(id2, Hash.From(0xDEADBEAF)),
            File.Size.Assert(id2, Size.From(77)),
            File.ModId.Assert(id1, modId1),
            File.ModId.Assert(id2, modId1),
            Mod.Name.Assert(modId1, "Test Mod 1"),
            Mod.LoadoutId.Assert(modId1, loadoutId),
            Mod.Name.Assert(modId2, "Test Mod 2"),
            Mod.LoadoutId.Assert(modId2, loadoutId),
            Loadout.Name.Assert(loadoutId, "Test Loadout 1"),
            Collection.Name.Assert(collectionId, "Test Collection 1"),
            Collection.Loadout.Assert(collectionId, loadoutId),
            Collection.Mods.Assert(collectionId, modId1),
            Collection.Mods.Assert(collectionId, modId2)
        ]);

        id1 = tx.Remaps[id1];
        id2 = tx.Remaps[id2];
        modId2 = tx.Remaps[modId2];
        collectionId = tx.Remaps[collectionId];

        tx = await DatomStore.Transact([
            // Rename file 1 and move file 1 to mod 2
            File.Path.Assert(id2, "/foo/qux"),
            File.ModId.Assert(id1, modId2),
            // Remove mod2 from collection
            Collection.Mods.Retract(collectionId, modId2),
        ]);
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

        var tx1 = await DatomStore.Transact([
            File.Path.Assert(id, "/foo/bar"),
            File.Hash.Assert(id, Hash.From(0xDEADBEEF)),
            File.Size.Assert(id, Size.From(42)),
            File.ModId.Assert(id, modId),
        ]);

        id = tx1.Remaps[id];
        modId = tx1.Remaps[modId];

        var tx2 = await DatomStore.Transact([
            File.Path.Retract(id, "/foo/bar"),
            File.Hash.Retract(id, Hash.From(0xDEADBEEF)),
            File.Size.Retract(id, Size.From(42)),
            File.ModId.Retract(id, modId)
        ]);


        var datoms = tx2.Snapshot
            .Datoms(type)
            .Select(d => d.Resolved)
            .ToArray();
        await Verify(datoms.ToTable(Registry))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }
}
