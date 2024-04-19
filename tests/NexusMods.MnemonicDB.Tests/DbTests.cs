using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;


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
            var file = new File.Model(tx)
            {
                Path = $"C:\\test_{idx}.txt",
                Hash = Hash.From(idx + 0xDEADBEEF),
                Size = Size.From(idx),
                ModId = EntityId.From(1)
            };
            ids.Add(file.Id);
        }

        var oldTx = Connection.TxId;
        var result = await tx.Commit();

        await Task.Delay(1000);
        result.NewTx.Should().NotBe(oldTx, "transaction id should be incremented");
        result.NewTx.Value.Should().Be(oldTx.Value + 1, "transaction id should be incremented by 1");

        var db = Connection.Db;
        var resolved = db.Get<File.Model>(ids.Select(id => result[id])).ToArray();
        await VerifyModel(resolved);
    }

    [Fact]
    public async Task ReadDatomsOverTime()
    {
        var times = 3;
        var txEs = new List<TxId>();

        var tx = Connection.BeginTransaction();
        var file = new Mod.Model(tx)
        {
            Name = "Test Mod",
            Source = new Uri("http://test.com"),
            Loadout = new Loadout.Model(tx)
            {
                Name = "Test Loadout"
            }
        };
        var result = await tx.Commit();

        var modId = result[file.Id];
        txEs.Add(result.NewTx);

        for (var i = 0; i < times; i++)
        {
            var newTx = Connection.BeginTransaction();
            newTx.Add(modId, Mod.Name, $"Test Mod {i}");
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

        var file = new File.Model(tx)
        {
            Path = "C:\\test.txt",
            Hash = Hash.From(1 + 0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };


        var result = await tx.Commit();

        var realId = result[file.Id];
        var originalDb = Connection.Db;

        // Validate the data
        var found = originalDb.Get<File.Model>([realId]).First();
        await VerifyModel(found).UseTextForParameters("original data");


        // Mutate the data
        for (var i = 0; i < times; i++)
        {
            var newTx = Connection.BeginTransaction();
            newTx.Add(realId, File.Path, $"C:\\test_{i}.txt_mutate");

            await newTx.Commit();

            // Validate the data
            var newDb = Connection.Db;
            newDb.BasisTxId.Value.Should().Be(originalDb.BasisTxId.Value + 1UL + (ulong)i,
                "transaction id should be incremented by 1 for each mutation at iteration " + i);

            var newFound = newDb.Get<File.Model>([realId]).First();
            await VerifyModel(newFound).UseTextForParameters("mutated data " + i);

            // Validate the original data
            var orignalFound = originalDb.Get<File.Model>([realId]).First();
            await VerifyModel(orignalFound).UseTextForParameters("original data" + i);
        }
    }


    [Fact]
    public async Task ReadModelsCanHaveExtraAttributes()
    {
        // Insert some data
        var tx = Connection.BeginTransaction();
        var file = new File.Model(tx)
        {
            Path = "C:\\test.txt",
            Hash = Hash.From(1 + 0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        // Attach extra attributes to the entity

        tx.Add(file.Id, ArchiveFile.Path, "C:\\test.zip");
        tx.Add(file.Id, ArchiveFile.Hash, Hash.From(0xFEEDBEEF));

        var result = await tx.Commit();


        var realId = result[file.Id];
        var db = Connection.Db;

        // Original data exists
        var readModel = db.Get<File.Model>([realId]).First();
        await VerifyModel(readModel).UseTextForParameters("file data");


        // Extra data exists and can be read with a different read model
        var archiveReadModel = db.Get<ArchiveFile.Model>([realId]).First();
        await VerifyModel(archiveReadModel).UseTextForParameters("archive file data");
    }

    [Fact]
    public async Task CanGetCommitUpdates()
    {
        List<IReadDatom[]> updates = new();


        var tx = Connection.BeginTransaction();
        var file = new File.Model(tx)
        {
            Path = "C:\\test.txt",
            Hash = Hash.From((ulong)0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };
        var result = await tx.Commit();

        var realId = result[file.Id];

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
            tx.Add(realId, File.Hash, Hash.From(0xDEADBEEF + (ulong)idx + 0xEE));
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

        var loadout = new Loadout.Model(tx)
        {
            Name = "Test Loadout"
        };

        _ = new Mod.Model(tx)
        {
            Name = "Test Mod 1",
            Source = new Uri("http://mod1.com"),
            Loadout = loadout
        };

        _ = new Mod.Model(tx)
        {
            Name = "Test Mod 2",
            Source = new Uri("http://mod2.com"),
            Loadout = loadout
        };

        var result = await tx.Commit();

        var newDb = Connection.Db;

        loadout = newDb.Get<Loadout.Model>([result[loadout.Id]]).First();

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

    [Fact]
    public async Task CanFindEntitiesByAttribute()
    {
        await InsertExampleData();

        var db = Connection.Db;

        var ids = from id in db.Find(Mod.Name)
            let thisName = db.Get(id, Mod.Name)
            from byFind in db.FindIndexed(thisName, Mod.Name)
            select (id.Value.ToString("x"), thisName, byFind.Value.ToString("x"));

        await Verify(ids);
    }

    [Fact]
    public async Task CanGetDatomsFromEntity()
    {
        var loadout = await InsertExampleData();
        var mod = loadout.Mods.First();

        mod.Contains(Mod.Name).Should().BeTrue();
        mod.Contains(Mod.Source).Should().BeTrue();
        mod.Contains(Loadout.Name).Should().BeFalse();

        mod.ToString().Should().Be("Mod+Model<200000000000002>");

        await VerifyTable(mod.Select(d => d.Resolved));
    }

    [Fact]
    public async Task CanPutEntitiesInDifferentPartitions()
    {

        using var tx = Connection.BeginTransaction();
        var file1 = new File.Model(tx, (byte)Ids.Partition.Entity)
        {
            Path = "C:\\test1.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        var file2 = new File.Model(tx, (byte)Ids.Partition.Entity + 1)
        {
            Path = "C:\\test2.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        var file3 = new File.Model(tx, (byte)Ids.Partition.Entity + 200)
        {
            Path = "C:\\test3.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        // TempIds store the desired partition in the third highest byte
        (file1.Id.Value >> 40 & 0xFF).Should().Be((byte)Ids.Partition.Entity);
        (file2.Id.Value >> 40 & 0xFF).Should().Be((byte)Ids.Partition.Entity + 1);
        (file3.Id.Value >> 40 & 0xFF).Should().Be((byte)Ids.Partition.Entity + 200);

        var result = await tx.Commit();
        file1 = result.Remap(file1);
        file2 = result.Remap(file2);
        file3 = result.Remap(file3);


        var allDatoms = file1.Concat(file2).Concat(file3)
            .Select(f => f.Resolved);

        await VerifyTable(allDatoms);

    }

}
