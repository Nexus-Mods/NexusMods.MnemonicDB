using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;
using File = NexusMods.EventSourcing.TestModel.Model.File;

namespace NexusMods.EventSourcing.Tests;

public class DbTests : AEventSourcingTest
{

    public DbTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes, IEnumerable<IReadModelFactory> factories)
        : base(valueSerializers, attributes, factories)
    {

    }

    [Fact]
    public void ReadDatomsForEntity()
    {
        const int TOTAL_COUNT = 10;
        var tx = Connection.BeginTransaction();


        var ids = new List<EntityId>();
        for (ulong i = 0; i < TOTAL_COUNT; i++)
        {
            var fileId = tx.TempId();
            ids.Add(fileId);
            File.Path.Assert(fileId, $"C:\\test_{i}.txt", tx);
            File.Hash.Assert(fileId, i + 0xDEADBEEF, tx);
            File.Index.Assert(fileId, i, tx);
        }

        var oldTx = Connection.TxId;
        var result = tx.Commit();

        result.NewTx.Value.Should().Be(oldTx.Value + 1, "transaction id should be incremented by 1");

        var db = Connection.Db;
        var resolved = db.Get<FileReadModel>(ids.Select(id => result[id])).ToArray();

        resolved.Should().HaveCount(TOTAL_COUNT);
        foreach (var readModel in resolved)
        {
            var idx = readModel.Index;
            readModel.Hash.Should().Be(idx + 0xDEADBEEF);
            readModel.Path.Should().Be($"C:\\test_{idx}.txt");
        }

    }

}
