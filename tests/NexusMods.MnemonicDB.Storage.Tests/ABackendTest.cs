using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;


namespace NexusMods.MnemonicDB.Storage.Tests;

public abstract class ABackendTest(
    IServiceProvider provider,
    bool isInMemory)
    : AStorageTest(provider, isInMemory)
{
    [Test]
    [Arguments(IndexType.TxLog)]
    [Arguments(IndexType.EAVTHistory)]
    [Arguments(IndexType.EAVTCurrent)]
    [Arguments(IndexType.AEVTCurrent)]
    [Arguments(IndexType.AEVTHistory)]
    [Arguments(IndexType.VAETCurrent)]
    [Arguments(IndexType.VAETHistory)]
    [Arguments(IndexType.AVETCurrent)]
    [Arguments(IndexType.AVETHistory)]
    public async Task InsertedDatomsShowUpInTheIndex(IndexType type)
    {
        var tx = await GenerateData();
        var datoms = tx.Snapshot
            .Datoms(SliceDescriptor.Create(type));

        await Verify(datoms.ToTable(AttributeResolver))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }

    [Test]
    [Arguments(IndexType.TxLog)]
    [Arguments(IndexType.EAVTHistory)]
    [Arguments(IndexType.EAVTCurrent)]
    [Arguments(IndexType.AEVTCurrent)]
    [Arguments(IndexType.AEVTHistory)]
    [Arguments(IndexType.VAETCurrent)]
    [Arguments(IndexType.VAETHistory)]
    [Arguments(IndexType.AVETCurrent)]
    [Arguments(IndexType.AVETHistory)]
    public async Task CanStoreDataInBlobs(IndexType type)
    {
        // 256 bytes of data
        var smallData = Enumerable.Range(0, byte.MaxValue)
            .Select(v => (byte)v).ToArray();

        // 16MB of data
        var largeData = Enumerable.Range(0, 1024 * 1024 * 16)
            .Select(v => (byte)(v % 256))
            .ToArray();


        var ids = new List<EntityId>();

        for (var i = 0; i < 4; i++)
        {
            var segment = new Datoms(AttributeCache);
            var entityId = NextTempId();
            var (result, _) = await DatomStore.TransactAsync(new Datoms(DatomStore) {
                { entityId, Blobs.InKeyBlob, smallData },
                { entityId, Blobs.InValueBlob, largeData }
            });
            ids.Add(result.Remaps[entityId]);
        }

        // Retract the first 2
        for (var i = 0; i < 2; i++)
        {
            await DatomStore.TransactAsync(new Datoms(DatomStore) {
                { ids[i], Blobs.InKeyBlob, smallData.AsMemory(), true },
                { ids[i], Blobs.InValueBlob, largeData.AsMemory(), true }
            });
        }

        smallData[0] = 1;
        largeData[0] = 1;

        // Change the other 2
        for (var i = 5; i < 2; i++)
        {
            await DatomStore.TransactAsync(new Datoms(DatomStore) {
                { ids[i], Blobs.InKeyBlob, smallData.AsMemory(), true },
                { ids[i], Blobs.InValueBlob, largeData.AsMemory(), true },
            });
        }

        var datoms = DatomStore.GetSnapshot()
            .Datoms(SliceDescriptor.Create(type))
            .ToArray();

        await Verify(datoms.ToTable(AttributeResolver))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }


    [Test]
    [Arguments(IndexType.EAVTHistory)]
    [Arguments(IndexType.EAVTCurrent)]
    [Arguments(IndexType.AEVTCurrent)]
    [Arguments(IndexType.AEVTHistory)]
    [Arguments(IndexType.VAETCurrent)]
    [Arguments(IndexType.VAETHistory)]
    [Arguments(IndexType.AVETCurrent)]
    [Arguments(IndexType.AVETHistory)]
    public async Task HistoricalQueriesReturnAllDataSorted(IndexType type)
    {
        var tx = await GenerateData();
        var current = tx.Snapshot.Datoms(SliceDescriptor.Create(type.CurrentVariant()));
        var history = tx.Snapshot.Datoms(SliceDescriptor.Create(type.HistoryVariant()));
        var comparer = type.GetComparator();
        var merged = current
            .Merge(history, CompareDatoms(comparer))
            .ToArray();

        await Verify(merged.ToTable(AttributeResolver))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }

    [Test]
    public async Task CaseInsenstiveUTF8DoesntCrashTheComparator()
    {
        var id1 = NextTempId();
        var id2 = NextTempId();
        var id3 = NextTempId();

        var segment = new Datoms(AttributeCache)
        {
            { id1, File.Path, "foo/bar" },
            { id2, File.Path, "foo/bar" },
            { id3, File.Path, "foo/bar" }
        };

        var (tx, _) = await DatomStore.TransactAsync(segment);
    }

    private static Func<Datom, Datom, int> CompareDatoms(IDatomComparator comparer)
    {
        return (a, b) => comparer.CompareInstance(a, b);
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

            var segment = new Datoms(AttributeCache)
            {
                { id1, File.Path, "foo/bar" },
                { id1, File.Hash, Hash.From(0xDEADBEEF) },
                { id1, File.Size, Size.From(42) },
                { id2, File.Path, "qix/bar" },
                { id2, File.Hash, Hash.From(0xDEADBEAF) },
                { id2, File.Size, Size.From(77) },
                { id1, File.ModId, modId1 },
                { id2, File.ModId, modId1 },
                { modId1, Mod.Name, "Test Mod 1" },
                { modId1, Mod.LoadoutId, loadoutId },
                { modId2, Mod.Name, "Test Mod 2" },
                { modId2, Mod.LoadoutId, loadoutId },
                { loadoutId, Loadout.Name, "Test Loadout 1" },
                { collectionId, Collection.Name, "Test Collection 1" },
                { collectionId, Collection.LoadoutId, loadoutId },
                { collectionId, Collection.ModIds, modId1 },
                { collectionId, Collection.ModIds, modId2 }
            };
            
            (tx, _) = await DatomStore.TransactAsync(segment);
        }

        id1 = tx.Remaps[id1];
        id2 = tx.Remaps[id2];
        modId2 = tx.Remaps[modId2];
        collectionId = tx.Remaps[collectionId];

        {
            var segment = new Datoms(AttributeCache)
            {
                { id2, File.Path, "foo/qux" },
                { id1, File.ModId, modId2 },
                { collectionId, Collection.ModIds, modId2, true }
            };
            (tx, _) = await DatomStore.TransactAsync(segment);
        }
        return tx;
    }

    [Test]
    [Arguments(IndexType.TxLog)]
    [Arguments(IndexType.EAVTHistory)]
    [Arguments(IndexType.EAVTCurrent)]
    [Arguments(IndexType.AEVTCurrent)]
    [Arguments(IndexType.AEVTHistory)]
    [Arguments(IndexType.VAETCurrent)]
    [Arguments(IndexType.VAETHistory)]
    [Arguments(IndexType.AVETCurrent)]
    [Arguments(IndexType.AVETHistory)]
    public async Task RetractedValuesAreSupported(IndexType type)
    {
        var id = NextTempId();
        var modId = NextTempId();

        StoreResult tx1, tx2;

        {
            var segment = new Datoms(AttributeCache)
            {
                { id, File.Path, "foo/bar" },
                { id, File.Hash, Hash.From(0xDEADBEEF) },
                { id, File.Size, Size.From(42) },
                { id, File.ModId, modId }
            };

            (tx1, _) = await DatomStore.TransactAsync(segment);
        }

        id = tx1.Remaps[id];
        modId = tx1.Remaps[modId];

        {
            var segment = new Datoms(AttributeCache)
            {
                { id, File.Path, "foo/bar", true },
                { id, File.Hash, Hash.From(0xDEADBEEF), true },
                { id, File.Size, Size.From(42), true },
                { id, File.ModId, modId, true }
            };

            (tx2, _) = await DatomStore.TransactAsync(segment);

        }


        var datoms = tx2.Snapshot
            .Datoms(SliceDescriptor.Create(type))
            .ToArray();
        await Verify(datoms.ToTable(AttributeResolver))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }


    [Test]
    public async Task CanLoadExistingAttributes()
    {
        var attrs = DatomStore.GetSnapshot().Datoms(SliceDescriptor.Create(AttributeDefinition.UniqueId, AttributeCache))
            .ToArray();

        await Verify(attrs.ToTable(AttributeResolver));
    }

    [Test]
    [Arguments(IndexType.TxLog)]
    [Arguments(IndexType.EAVTHistory)]
    [Arguments(IndexType.EAVTCurrent)]
    [Arguments(IndexType.AEVTCurrent)]
    [Arguments(IndexType.AEVTHistory)]
    [Arguments(IndexType.VAETCurrent)]
    [Arguments(IndexType.VAETHistory)]
    [Arguments(IndexType.AVETCurrent)]
    [Arguments(IndexType.AVETHistory)]
    public async Task AsIfShowsAllDatoms(IndexType type)
    {
        var id = NextTempId();
        var id2 = NextTempId();
        var modId = NextTempId();

        StoreResult tx1, tx2;

        {
            var segment = new Datoms(AttributeCache)
            {
                { id, File.Path, "foo/bar" },
                { id, File.Hash, Hash.From(0xDEADBEEF) },
                { id, File.Size, Size.From(42) },
                { id, File.ModId, modId },
                
                { id2, File.Path, "foo/bar2" },
                { id2, File.Hash, Hash.From(0xDEADBEE2) },
                { id2, File.Size, Size.From(44) },
                { id2, File.ModId, modId }
            };

            (tx1, _) = await DatomStore.TransactAsync(segment);
        }

        id = tx1.Remaps[id];
        modId = tx1.Remaps[modId];

        var segment2 = new Datoms(AttributeCache)
        {
            { id, File.Path, "foo/bar", true },
            { id, File.Hash, Hash.From(0xDEADBEEF), true },
            { id, File.Size, Size.From(42), true },
            { id, File.ModId, modId, true },
            
            { id2, File.Size, Size.From(44) }
        };
        
        var asIf = DatomStore.GetSnapshot().AsIf(segment2);


        var datoms = asIf
            .Datoms(SliceDescriptor.Create(type))
            .ToArray();
        await Verify(datoms.ToTable(AttributeResolver))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }
}
