using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Storage.Abstractions;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.MneumonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.Paths;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;


namespace NexusMods.MneumonicDB.Storage.Tests;

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
        var id1 = NextTempId();
        var id2 = NextTempId();

        var modId1 = NextTempId();
        var modId2 = NextTempId();
        var loadoutId = NextTempId();
        var collectionId = NextTempId();

        var tx = await DatomStore.Transact([
            FileAttributes.Path.Assert(id1, "/foo/bar"),
            FileAttributes.Hash.Assert(id1, Hash.From(0xDEADBEEF)),
            FileAttributes.Size.Assert(id1, Size.From(42)),
            FileAttributes.Path.Assert(id2, "/qix/bar"),
            FileAttributes.Hash.Assert(id2, Hash.From(0xDEADBEAF)),
            FileAttributes.Size.Assert(id2, Size.From(77)),
            FileAttributes.ModId.Assert(id1, modId1),
            FileAttributes.ModId.Assert(id2, modId1),
            ModAttributes.Name.Assert(modId1, "Test Mod 1"),
            ModAttributes.LoadoutId.Assert(modId1, loadoutId),
            ModAttributes.Name.Assert(modId2, "Test Mod 2"),
            ModAttributes.LoadoutId.Assert(modId2, loadoutId),
            LoadoutAttributes.Name.Assert(loadoutId, "Test Loadout 1"),
            CollectionAttributes.Name.Assert(collectionId, "Test Collection 1"),
            CollectionAttributes.LoadoutId.Assert(collectionId, loadoutId),
            CollectionAttributes.Mods.Assert(collectionId, modId1),
            CollectionAttributes.Mods.Assert(collectionId, modId2)
        ]);

        id1 = tx.Remaps[id1];
        id2 = tx.Remaps[id2];
        modId2 = tx.Remaps[modId2];
        collectionId = tx.Remaps[collectionId];

        tx = await DatomStore.Transact([
            // Rename file 1 and move file 1 to mod 2
            FileAttributes.Path.Assert(id2, "/foo/qux"),
            FileAttributes.ModId.Assert(id1, modId2),
            // Remove mod2 from collection
            CollectionAttributes.Mods.Retract(collectionId, modId2),
        ]);

        using var iterator = tx.Snapshot.GetIterator(type);
        await Verify(iterator.SeekStart().Resolve().ToTable(Registry))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
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
            FileAttributes.Path.Assert(id, "/foo/bar"),
            FileAttributes.Hash.Assert(id, Hash.From(0xDEADBEEF)),
            FileAttributes.Size.Assert(id, Size.From(42)),
            FileAttributes.ModId.Assert(id, modId),
        ]);

        id = tx1.Remaps[id];
        modId = tx1.Remaps[modId];

        var tx2 = await DatomStore.Transact([
            FileAttributes.Path.Retract(id, "/foo/bar"),
            FileAttributes.Hash.Retract(id, Hash.From(0xDEADBEEF)),
            FileAttributes.Size.Retract(id, Size.From(42)),
            FileAttributes.ModId.Retract(id, modId)
        ]);


        using var iterator = tx2.Snapshot.GetIterator(type);
        await Verify(iterator.SeekStart().Resolve().ToTable(Registry))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }
}
