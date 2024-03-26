using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.RocksDbBackend;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;
using NexusMods.EventSourcing.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using FileAttributes = NexusMods.EventSourcing.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.EventSourcing.Storage.Tests.BackendTests;

public class RocksDB(IServiceProvider provider) : ABackendTest<Backend>(provider, (registry) => new Backend(registry))
{

    [Theory]
    [InlineData(IndexType.TxLog)]
    [InlineData(IndexType.EAVTHistory)]
    [InlineData(IndexType.EAVTCurrent)]
    [InlineData(IndexType.AEVTCurrent)]
    [InlineData(IndexType.AEVTHistory)]
    public async Task InsertedDatomsShowUpInTheIndex(IndexType type)
    {
        var id1 = NextTempId();
        var id2 = NextTempId();

        var modId = NextTempId();

        var tx = await DatomStore.Transact([
            FileAttributes.Path.Assert(id1, "/foo/bar"),
            FileAttributes.Hash.Assert(id1, Hash.From(0xDEADBEEF)),
            FileAttributes.Size.Assert(id1, Paths.Size.From(42)),
            FileAttributes.Path.Assert(id2, "/qix/bar"),
            FileAttributes.Hash.Assert(id2, Hash.From(0xDEADBEAF)),
            FileAttributes.Size.Assert(id2, Paths.Size.From(77)),
            FileAttributes.ModId.Assert(id1, modId),
            FileAttributes.ModId.Assert(id2, modId),
            ModAttributes.Name.Assert(modId, "Test Mod"),
        ]);

        id1 = tx.Remaps[id1];

        tx = await DatomStore.Transact([
            FileAttributes.Path.Assert(id1, "/foo/qux")
        ]);

        using var snapshot = DatomStore.GetSnapshot();
        var results = DatomStore.SeekIndex(snapshot, type).ToList();

        await Verify(results.ToTable(Registry))
            .UseParameters(type);
    }

}
