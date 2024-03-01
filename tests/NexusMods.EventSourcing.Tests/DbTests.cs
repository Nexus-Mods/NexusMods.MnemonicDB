using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.EventSourcing.TestModel.Model.Attributes;
using Xunit.Sdk;
using File = NexusMods.EventSourcing.TestModel.Model.File;

namespace NexusMods.EventSourcing.Tests;

public class DbTests(IServiceProvider provider) : AEventSourcingTest(provider)
{
    [Fact]
    public async Task ReadDatomsForEntity()
    {
        const int totalCount = 10;
        var tx = Connection.BeginTransaction();


        var ids = new List<EntityId>();
        for (ulong idx = 0; idx < totalCount; idx++)
        {
            var file = new File(tx)
            {
                Path = $"C:\\test_{idx}.txt",
                Hash = idx + 0xDEADBEEF,
                Index = idx
            };
            ids.Add(file.Id);
        }

        var oldTx = Connection.TxId;
        var result = await tx.Commit();

        await Task.Delay(1000);
        result.NewTx.Should().NotBe(oldTx, "transaction id should be incremented");
        result.NewTx.Value.Should().Be(oldTx.Value + 1, "transaction id should be incremented by 1");

        var db = Connection.Db;
        var resolved = db.Get<File>(ids.Select(id => result[id])).ToArray();

        resolved.Should().HaveCount(totalCount);
        foreach (var readModel in resolved)
        {
            var idx = readModel.Index;
            readModel.Hash.Should().Be(idx + 0xDEADBEEF);
            readModel.Path.Should().Be($"C:\\test_{idx}.txt");
            readModel.Index.Should().Be(idx);
        }
    }


    [Fact]
    public async Task DbIsImmutable()
    {
        const int times = 10;

        // Insert some data
        var tx = Connection.BeginTransaction();
        var file = new File(tx)
        {
            Path = "C:\\test.txt_mutate",
            Hash = 0xDEADBEEF,
            Index = 0
        };

        var result = await tx.Commit();

        var realId = result[file.Id];
        var originalDb = Connection.Db;

        // Validate the data
        var found = originalDb.Get<File>([realId]).First();
        found.Path.Should().Be("C:\\test.txt_mutate");
        found.Hash.Should().Be(0xDEADBEEF);
        found.Index.Should().Be(0);

        // Mutate the data
        for (var i = 0; i < times; i++)
        {
            var newTx = Connection.BeginTransaction();
            ModFileAttributes.Path.Add(newTx, realId, $"C:\\test_{i}.txt_mutate");

            await newTx.Commit();

            // Validate the data
            var newDb = Connection.Db;
            newDb.BasisTxId.Value.Should().Be(originalDb.BasisTxId.Value + 1UL + (ulong)i, "transaction id should be incremented by 1 for each mutation");

            var newFound = newDb.Get<File>([realId]).First();
            newFound.Path.Should().Be($"C:\\test_{i}.txt_mutate");

            // Validate the original data
            var orignalFound = originalDb.Get<File>([realId]).First();
            orignalFound.Path.Should().Be("C:\\test.txt_mutate");
        }
    }


    [Fact]
    public async Task ReadModelsCanHaveExtraAttributes()
    {
        // Insert some data
        var tx = Connection.BeginTransaction();
        var file = new File(tx)
        {
            Path = "C:\\test.txt",
            Hash = 0xDEADBEEF,
            Index = 77
        };
        // Attach extra attributes to the entity
        ArchiveFileAttributes.Path.Add(tx, file.Id, "C:\\test.zip");
        ArchiveFileAttributes.ArchiveHash.Add(tx, file.Id, 0xFEEDBEEF);
        var result = await tx.Commit();


        var realId = result[file.Id];
        var db = Connection.Db;
        // Original data exists
        var readModel = db.Get<File>([realId]).First();
        readModel.Path.Should().Be("C:\\test.txt");
        readModel.Hash.Should().Be(0xDEADBEEF);
        readModel.Index.Should().Be(77);

        // Extra data exists and can be read with a different read model
        var archiveReadModel = db.Get<ArchiveFile>([realId]).First();
        archiveReadModel.ModPath.Should().Be("C:\\test.txt");
        archiveReadModel.Path.Should().Be("C:\\test.zip");
        archiveReadModel.Hash.Should().Be(0xFEEDBEEF);
        archiveReadModel.Index.Should().Be(77);
    }

    [Fact]
    public void CanGetCommitUpdates()
    {

        throw new NotImplementedException();
        /*
        List<IDatom[]> updates = new();

        Connection.Commits.Subscribe(update =>
        {
            updates.Add(update.Datoms.ToArray());
        });

        var tx = Connection.BeginTransaction();
        var file = new File(tx)
        {
            Path = "C:\\test.txt",
            Hash = 0xDEADBEEF,
            Index = 77
        };
        var result = tx.Commit();

        var realId = result[file.Id];

        updates.Should().HaveCount(1);

        for (var idx = 0; idx < 10; idx++)
        {
            tx = Connection.BeginTransaction();
            ModFileAttributes.Index.Add(tx, realId, (ulong)idx);
            result = tx.Commit();

            result.Datoms.Should().BeEquivalentTo(updates[idx + 1]);

            updates.Should().HaveCount(idx + 2);
            var updateDatom = updates[idx + 1].OfType<AssertDatom<ModFileAttributes.Index, ulong>>()
                .First();
            updateDatom.V.Should().Be((ulong)idx);
        }
        */

    }

    [Fact]
    public async Task CanGetChildEntities()
    {
        var tx = Connection.BeginTransaction();
        var loadout = Loadout.Create(tx, "Test Loadout");
        Mod.Create(tx, "Test Mod 1", loadout.Id);
        Mod.Create(tx, "Test Mod 2", loadout.Id);
        var result = await tx.Commit();

        var newDb = Connection.Db;

        loadout = newDb.Get<Loadout>([result[loadout.Id]]).First();

        loadout.Mods.Count().Should().Be(2);
        loadout.Mods.Select(m => m.Name).Should().BeEquivalentTo(["Test Mod 1", "Test Mod 2"]);

        var firstMod = loadout.Mods.First();
        Ids.IsPartition(firstMod.Loadout.Id.Value, Ids.Partition.Entity)
            .Should()
            .Be(true, "the temp id should be replaced with a real id");
        firstMod.Loadout.Id.Should().Be(loadout.Id);
        firstMod.Db.Should().Be(newDb);
        loadout.Name.Should().Be("Test Loadout");
        firstMod.Loadout.Name.Should().Be("Test Loadout");
    }


}
