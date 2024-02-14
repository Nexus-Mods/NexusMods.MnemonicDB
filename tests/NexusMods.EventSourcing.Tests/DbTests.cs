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

    [Fact]
    public void DbIsImmutable()
    {
        const int TIMES = 10;

        // Insert some data
        var tx = Connection.BeginTransaction();
        var fileId = tx.TempId();
        File.Path.Assert(fileId, "C:\\test.txt_mutate", tx);
        File.Hash.Assert(fileId, 0xDEADBEEF, tx);
        File.Index.Assert(fileId, 0, tx);

        var result = tx.Commit();

        var realId = result[fileId];
        var originalDb = Connection.Db;

        // Validate the data
        var found = originalDb.Get<FileReadModel>([realId]).First();
        found.Path.Should().Be("C:\\test.txt_mutate");
        found.Hash.Should().Be(0xDEADBEEF);
        found.Index.Should().Be(0);

        // Mutate the data
        for (var i = 0; i < TIMES; i++)
        {
            var newTx = Connection.BeginTransaction();
            File.Path.Assert(fileId, $"C:\\test_{i}.txt_mutate", newTx);

            var newResult = newTx.Commit();

            // Validate the data
            var newDb = Connection.Db;
            var newId = newResult[fileId];
            var newFound = newDb.Get<FileReadModel>([newId]).First();
            newFound.Path.Should().Be($"C:\\test_{i}.txt_mutate");

            // Validate the original data
            var orignalFound = originalDb.Get<FileReadModel>([realId]).First();
            orignalFound.Path.Should().Be("C:\\test.txt_mutate");
        }
    }

    [Fact]
    public void ReadModelsCanHaveExtraAttributes()
    {
        var tx = Connection.BeginTransaction();
        var fileId = tx.TempId();
        File.Path.Assert(fileId, "C:\\test.txt", tx);
        File.Hash.Assert(fileId, 0xDEADBEEF, tx);
        File.Index.Assert(fileId, 77, tx);
        ArchiveFile.Index.Assert(fileId, 42, tx);
        ArchiveFile.ArchivePath.Assert(fileId, "C:\\archive.zip", tx);
        var result = tx.Commit();

        var realId = result[fileId];
        var db = Connection.Db;
        var readModel = db.Get<FileReadModel>([realId]).First();
        readModel.Path.Should().Be("C:\\test.txt");
        readModel.Hash.Should().Be(0xDEADBEEF);
        readModel.Index.Should().Be(77);

        var archiveReadModel = db.Get<ArchiveFileReadModel>([realId]).First();
        archiveReadModel.Path.Should().Be("C:\\test.txt");
        archiveReadModel.Hash.Should().Be(0xDEADBEEF);
        archiveReadModel.Index.Should().Be(42);
        archiveReadModel.ArchivePath.Should().Be("C:\\archive.zip");


    }

}
