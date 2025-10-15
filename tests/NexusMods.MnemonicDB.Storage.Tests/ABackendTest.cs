using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
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

        await Verify(datoms.ToTable(AttributeCache))
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
            using var segment = new IndexSegmentBuilder(AttributeCache);
            var entityId = NextTempId();
            var (result, _) = await DatomStore.TransactAsync([
                ValueDatom.Create(entityId, Blobs.InKeyBlob, smallData, DatomStore.AttributeCache),
                ValueDatom.Create(entityId, Blobs.InValueBlob, largeData, DatomStore.AttributeCache)
            ]);
            ids.Add(result.Remaps[entityId]);
        }

        // Retract the first 2
        for (var i = 0; i < 2; i++)
        {
            await DatomStore.TransactAsync([
                ValueDatom.Create(ids[i], Blobs.InKeyBlob, smallData.AsMemory(), true, DatomStore.AttributeCache),
                ValueDatom.Create(ids[i], Blobs.InValueBlob, largeData.AsMemory(), true, DatomStore.AttributeCache)
            ]);
        }

        smallData[0] = 1;
        largeData[0] = 1;

        // Change the other 2
        for (var i = 5; i < 2; i++)
        {
            await DatomStore.TransactAsync([
                ValueDatom.Create(ids[i], Blobs.InKeyBlob, smallData.AsMemory(), true, DatomStore.AttributeCache),
                ValueDatom.Create(ids[i], Blobs.InValueBlob, largeData.AsMemory(), true, DatomStore.AttributeCache)
            ]);
        }

        var datoms = DatomStore.GetSnapshot()
            .Datoms(SliceDescriptor.Create(type))
            .ToArray();

        await Verify(datoms.ToTable(AttributeCache))
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
        throw new NotImplementedException();
        /*
        var tx = await GenerateData();
        var current = tx.Snapshot.Datoms(SliceDescriptor.Create(type.CurrentVariant()));
        var history = tx.Snapshot.Datoms(SliceDescriptor.Create(type.HistoryVariant()));
        var comparer = type.GetComparator();
        var merged = current
            .Merge(history, CompareDatoms(comparer))
            .ToArray();

        await Verify(merged.ToTable(AttributeCache))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
            */
    }

    [Test]
    public async Task CaseInsenstiveUTF8DoesntCrashTheComparator()
    {
        throw new NotImplementedException();
        /*
        using var segment = new IndexSegmentBuilder(AttributeCache);
        var id1 = NextTempId();
        var id2 = NextTempId();
        var id3 = NextTempId();
        segment.Add(id1, File.Path, "foo/bar");
        segment.Add(id2, File.Path, "foo/bar");
        segment.Add(id3, File.Path, "foo/bar");
        
        var (tx, _) = await DatomStore.TransactAsync(segment.Build());
        */
    }

    private static Func<Datom, Datom, int> CompareDatoms(IDatomComparator comparer)
    {
        return (a, b) => comparer.CompareInstance(a, b);
    }


    private async Task<StoreResult> GenerateData()
    {
        throw new NotSupportedException();
        /*
    var id1 = NextTempId();
    var id2 = NextTempId();

    var modId1 = NextTempId();
    var modId2 = NextTempId();
    var loadoutId = NextTempId();
    var collectionId = NextTempId();


    StoreResult tx;

    {

        using var segment = new IndexSegmentBuilder(AttributeCache);

        segment.Add(id1, File.Path, "foo/bar");
        segment.Add(id1, File.Hash, Hash.From(0xDEADBEEF));
        segment.Add(id1, File.Size, Size.From(42));
        segment.Add(id2, File.Path, "qix/bar");
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
        segment.Add(collectionId, Collection.LoadoutId, loadoutId);
        segment.Add(collectionId, Collection.ModIds, modId1);
        segment.Add(collectionId, Collection.ModIds, modId2);


        (tx, _) = await DatomStore.TransactAsync(segment.Build());
    }

    id1 = tx.Remaps[id1];
    id2 = tx.Remaps[id2];
    modId2 = tx.Remaps[modId2];
    collectionId = tx.Remaps[collectionId];

    {
        using var segment = new IndexSegmentBuilder(AttributeCache);
        segment.Add(id2, File.Path, "foo/qux");
        segment.Add(id1, File.ModId, modId2);
        segment.Add(collectionId, Collection.ModIds, modId2, true);
        (tx, _) = await DatomStore.TransactAsync(segment.Build());
    }
    return tx;
    */
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
        throw new NotImplementedException();
        /*
        var id = NextTempId();
        var modId = NextTempId();

        StoreResult tx1, tx2;

        {
            using var segment = new IndexSegmentBuilder(AttributeCache);

            segment.Add(id, File.Path, "foo/bar");
            segment.Add(id, File.Hash, Hash.From(0xDEADBEEF));
            segment.Add(id, File.Size, Size.From(42));
            segment.Add(id, File.ModId, modId);

            (tx1, _) = await DatomStore.TransactAsync(segment.Build());
        }

        id = tx1.Remaps[id];
        modId = tx1.Remaps[modId];

        {
            using var segment = new IndexSegmentBuilder(AttributeCache);

            segment.Add(id, File.Path, "foo/bar", true);
            segment.Add(id, File.Hash, Hash.From(0xDEADBEEF), true);
            segment.Add(id, File.Size, Size.From(42), true);
            segment.Add(id, File.ModId, modId, true);

            (tx2, _) = await DatomStore.TransactAsync(segment.Build());

        }


        var datoms = tx2.Snapshot
            .Datoms(SliceDescriptor.Create(type))
            .ToArray();
        await Verify(datoms.ToTable(AttributeCache))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
            */
    }


    [Test]
    public async Task CanLoadExistingAttributes()
    {
        var attrs = DatomStore.GetSnapshot().Datoms(SliceDescriptor.Create(AttributeDefinition.UniqueId, AttributeCache))
            .ToArray();

        await Verify(attrs.ToTable(AttributeCache));
    }
}
