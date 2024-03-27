using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;
using NexusMods.EventSourcing.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using FileAttributes = NexusMods.EventSourcing.TestModel.ComplexModel.Attributes.FileAttributes;


namespace NexusMods.EventSourcing.Storage.Tests;

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
            ModAttributes.Name.Assert(modId2, "Test Mod 2")
        ]);

        id1 = tx.Remaps[id1];
        id2 = tx.Remaps[id2];

        tx = await DatomStore.Transact([
            // Rename file 1 and move file 1 to mod 2
            FileAttributes.Path.Assert(id2, "/foo/qux"),
            FileAttributes.ModId.Assert(id1, modId2)
        ]);

        var snapshot = DatomStore.GetSnapshot();
        var results = DatomStore.Datoms(snapshot, type).ToList();

        await Verify(results.ToTable(Registry))
            .UseDirectory("BackendTestVerifyData")
            .UseParameters(type);
    }
}
