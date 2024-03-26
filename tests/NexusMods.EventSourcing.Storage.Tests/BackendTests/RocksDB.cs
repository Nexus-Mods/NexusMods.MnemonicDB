using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.RocksDbBackend;
using NexusMods.Hashing.xxHash64;
using FileAttributes = NexusMods.EventSourcing.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.EventSourcing.Storage.Tests.BackendTests;

public class RocksDB(IServiceProvider provider) : ABackendTest<Backend>(provider, (registry) => new Backend(registry))
{

    [Fact]
    public async Task InsertedDatomsShowUpInEAVTIndex()
    {
        var id = NextTempId();

        var tx = await DatomStore.Transact([
            FileAttributes.Path.Assert(id, "/foo/bar"),
            FileAttributes.Hash.Assert(id, Hash.From(0xDEADBEEF)),
            FileAttributes.Size.Assert(id, Paths.Size.From(42)),
        ]);

        id = tx.Remaps[id];

        using var snapshot = DatomStore.GetSnapshot();
        var results = DatomStore.SeekIndex(snapshot, IndexType.EAVTHistory, entityId: id).ToList();

        results.Should().HaveCount(3);
    }

}
