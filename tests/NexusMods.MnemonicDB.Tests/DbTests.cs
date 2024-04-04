using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels;
using NexusMods.MnemonicDB.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels.File;
using FileAttributes = NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MnemonicDB.Tests;

public class DbTests(IServiceProvider provider) : AMnemonicDBTest(provider)
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
                Hash = Hash.From(idx + 0xDEADBEEF),
                Size = Size.From(idx),
                ModId = EntityId.From(1)
            };
            ids.Add(file.Header.Id);
        }

        var oldTx = Connection.TxId;
        var result = await tx.Commit();

        await Task.Delay(1000);
        result.NewTx.Should().NotBe(oldTx, "transaction id should be incremented");
        result.NewTx.Value.Should().Be(oldTx.Value + 1, "transaction id should be incremented by 1");

        var db = Connection.Db;
        var resolved = db.Get<File>(ids.Select(id => result[id])).ToArray();
        await VerifyModel(resolved);
    }

    [Fact]
    public async Task ReadDatomsOverTime()
    {
        var times = 3;
        var txEs = new List<TxId>();

        var tx = Connection.BeginTransaction();
        var file = new Mod(tx)
        {
            Name = "Test Mod",
            Source = new Uri("http://test.com"),
            Loadout = new Loadout(tx)
            {
                Name = "Test Loadout"
            }
        };
        var result = await tx.Commit();

        var modId = result[file.Header.Id];
        txEs.Add(result.NewTx);

        for (var i = 0; i < times; i++)
        {
            var newTx = Connection.BeginTransaction();
            ModAttributes.Name.Add(newTx, modId, $"Test Mod {i}");
            result = await newTx.Commit();
            txEs.Add(result.NewTx);
        }

        foreach (var txId in txEs)
        {
            var db = Connection.AsOf(txId);
            var resolved = db.Datoms(modId);
            await VerifyTable(resolved).UseTextForParameters("mod data_" + txId.Value);
        }
    }


    [Fact]
    public async Task DbIsImmutable()
    {
        const int times = 3;

        // Insert some data
        var tx = Connection.BeginTransaction();

        var file = new File(tx)
        {
            Path = "C:\\test.txt",
            Hash = Hash.From(1 + 0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };


        var result = await tx.Commit();

        var realId = result[file.Header.Id];
        var originalDb = Connection.Db;

        // Validate the data
        var found = originalDb.Get<File>([realId]).First();
        await VerifyModel(found).UseTextForParameters("original data");


        // Mutate the data
        for (var i = 0; i < times; i++)
        {
            var newTx = Connection.BeginTransaction();
            FileAttributes.
                Path.Add(newTx, realId, $"C:\\test_{i}.txt_mutate");

            await newTx.Commit();

            // Validate the data
            var newDb = Connection.Db;
            newDb.BasisTxId.Value.Should().Be(originalDb.BasisTxId.Value + 1UL + (ulong)i,
                "transaction id should be incremented by 1 for each mutation at iteration " + i);

            var newFound = newDb.Get<File>([realId]).First();
            await VerifyModel(newFound).UseTextForParameters("mutated data " + i);

            // Validate the original data
            var orignalFound = originalDb.Get<File>([realId]).First();
            await VerifyModel(orignalFound).UseTextForParameters("original data" + i);
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
            Hash = Hash.From(1 + 0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        // Attach extra attributes to the entity
        ArchiveFileAttributes.Path.Add(tx, file.Header.Id, "C:\\test.zip");
        ArchiveFileAttributes.Hash.Add(tx, file.Header.Id, Hash.From(0xFEEDBEEF));
        var result = await tx.Commit();


        var realId = result[file.Header.Id];
        var db = Connection.Db;

        // Original data exists
        var readModel = db.Get<File>([realId]).First();
        await VerifyModel(readModel).UseTextForParameters("file data");


        // Extra data exists and can be read with a different read model
        var archiveReadModel = db.Get<ArchiveFile>([realId]).First();
        await VerifyModel(archiveReadModel).UseTextForParameters("archive file data");
    }

    [Fact]
    public async Task CanGetCommitUpdates()
    {
        List<IReadDatom[]> updates = new();


        var tx = Connection.BeginTransaction();
        var file = new File(tx)
        {
            Path = "C:\\test.txt",
            Hash = Hash.From((ulong)0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };
        var result = await tx.Commit();

        var realId = result[file.Header.Id];

        Connection.Revisions.Subscribe(update =>
        {
            var datoms = update.Datoms(update.BasisTxId).ToArray();
            // Only Txes we care about
            if (datoms.Any(d => d.E == realId))
                updates.Add(datoms);
        });

        for (var idx = 0; idx < 4; idx++)
        {
            tx = Connection.BeginTransaction();
            FileAttributes.Hash.Add(tx, realId, Hash.From(0xDEADBEEF + (ulong)idx + 0xEE));
            result = await tx.Commit();

            await Task.Delay(100);

            updates.Should().HaveCount(idx + 1);
            var updateDatom = updates[idx];

            await VerifyTable(updateDatom)
                .UseTextForParameters("update_datom_" + idx);
        }
    }

    [Fact]
    public async Task CanGetChildEntities()
    {
        var tx = Connection.BeginTransaction();

        var loadout = new Loadout(tx)
        {
            Name = "Test Loadout"
        };

        _ = new Mod(tx)
        {
            Name = "Test Mod 1",
            Source = new Uri("http://mod1.com"),
            Loadout = loadout
        };

        _ = new Mod(tx)
        {
            Name = "Test Mod 2",
            Source = new Uri("http://mod2.com"),
            Loadout = loadout
        };

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
        firstMod.Header.Db.Should().Be(newDb);
        loadout.Name.Should().Be("Test Loadout");
        firstMod.Loadout.Name.Should().Be("Test Loadout");
    }

}
