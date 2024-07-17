using System.Reactive.Linq;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
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
            var file = new File.New(tx)
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

        result.NewTx.Should().NotBe(oldTx, "transaction id should be incremented");
        result.NewTx.Value.Should().Be(oldTx.Value + 1, "transaction id should be incremented by 1");

        var db = Connection.Db;
        var resolved = File.Load(db, ids.Select(id => result[id]));
        await VerifyModel(resolved);
    }

    [Fact]
    public async Task ReadDatomsOverTime()
    {
        var times = 3;
        var txEs = new List<TxId>();

        var tx = Connection.BeginTransaction();
        var file = new Mod.New(tx)
        {
            Name = "Test Mod",
            Source = new Uri("http://test.com"),
            LoadoutId = new Loadout.New(tx)
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
            var resolved = db.Datoms(modId).Resolved();
            await VerifyTable(resolved).UseTextForParameters("mod data_" + txId.Value);
        }
    }


    [Fact]
    public async Task DbIsImmutable()
    {
        const int times = 3;

        // Insert some data
        var tx = Connection.BeginTransaction();

        var file = new File.New(tx)
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
        var found = File.Load(originalDb, realId);
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

            var newFound = File.Load(newDb, realId);
            await VerifyModel(newFound).UseTextForParameters("mutated data " + i);

            // Validate the original data
            var orignalFound = File.Load(originalDb, realId);
            await VerifyModel(orignalFound).UseTextForParameters("original data" + i);
        }
    }


    [Fact]
    public async Task ReadModelsCanHaveExtraAttributes()
    {
        // Insert some data
        var tx = Connection.BeginTransaction();

        var archiveFile = new ArchiveFile.New(tx, out var id)
        {
            File = new File.New(tx, id)
            {
                Path = "C:\\test.txt",
                Hash = Hash.From(1 + 0xDEADBEEF),
                Size = Size.From(1),
                ModId = EntityId.From(1)
            },
            Path = "C:\\test.zip",
            Hash = Hash.From(0xFEEDBEEF)
        };

        var result = await tx.Commit();


        var realId = result[archiveFile.Id];
        var db = Connection.Db;

        // Original data exists
        var readModel = File.Load(db, realId);
        await VerifyModel(readModel).UseTextForParameters("file data");


        // Extra data exists and can be read with a different read model
        var archiveReadModel = ArchiveFile.Load(db, realId);
        await VerifyModel(archiveReadModel).UseTextForParameters("archive file data");

        readModel.Id.Should().Be(archiveReadModel.Id, "both models are the same entity");

        archiveReadModel.AsFile().ToArray().Should().BeEquivalentTo(readModel.ToArray(), "archive file should have the same base data as the file");

        readModel.TryGetAsArchiveFile(out var castedDown).Should().BeTrue();
        (castedDown is ArchiveFile.ReadOnly).Should().BeTrue();

        var badCast = new File.ReadOnly(result.Db, EntityId.From(1));
        badCast.IsValid().Should().BeFalse("bad cast should not validate");
        badCast.TryGetAsArchiveFile(out var archiveFileBad).Should().BeFalse("bad cast should not be able to cast down");
        archiveFileBad.IsValid().Should().BeFalse("bad cast should not validate as archive file");

        castedDown.Should().BeEquivalentTo(archiveReadModel, "casted down model should be the same as the original model");
    }

    [Fact]
    public async Task CanGetCommitUpdates()
    {
        List<IReadDatom[]> updates = new();


        var tx = Connection.BeginTransaction();
        var file = new File.New(tx)
        {
            Path = "C:\\test.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };
        var result = await tx.Commit();

        var realId = result[file.Id];

        Connection.Revisions.Subscribe(update =>
        {
            // Only Txes we care about
            if (update.RecentlyAdded.Any(d => d.E == realId))
                updates.Add(update.RecentlyAdded.Select(d => d.Resolved).ToArray());
        });

        for (var idx = 0; idx < 4; idx++)
        {
            tx = Connection.BeginTransaction();
            tx.Add(realId, File.Hash, Hash.From(0xDEADBEEF + (ulong)idx + 0xEE));
            result = await tx.Commit();

            await Task.Delay(100);

            // +2 because we always get one update for the initial state and one for the new state
            updates.Should().HaveCount(idx + 2);
            var updateDatom = updates[idx + 1];

            await VerifyTable(updateDatom)
                .UseTextForParameters("update_datom_" + idx);
        }
    }

    [Fact]
    public async Task CanGetChildEntities()
    {
        var tx = Connection.BeginTransaction();

        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout"
        };

        _ = new Mod.New(tx)
        {
            Name = "Test Mod 1",
            Source = new Uri("http://mod1.com"),
            LoadoutId = loadout
        };

        _ = new Mod.New(tx)
        {
            Name = "Test Mod 2",
            Source = new Uri("http://mod2.com"),
            LoadoutId = loadout
        };

        var result = await tx.Commit();

        var newDb = Connection.Db;

        var loadoutWritten = result.Remap(loadout);

        loadoutWritten.Mods.Count.Should().Be(2);
        loadoutWritten.Mods.Select(m => m.Name).Should().BeEquivalentTo(["Test Mod 1", "Test Mod 2"]);

        var firstMod = loadoutWritten.Mods.First();
        firstMod.Loadout.Id.InPartition(PartitionId.Entity).Should().BeTrue("LoadoutId should in the entity partition");
        firstMod.LoadoutId.Should().BeEquivalentTo(loadoutWritten.LoadoutId);
        firstMod.Db.Should().Be(newDb);
        loadout.Name.Should().Be("Test Loadout");
        firstMod.Loadout.Name.Should().Be("Test Loadout");
    }


    [Fact]
    public async Task CanFindEntitiesByAttribute()
    {
        await InsertExampleData();

        var db = Connection.Db;

        var ids = from mod in Mod.All(db)
                  from modOther in Mod.FindByName(db, mod.Name)
                  select (mod.Name, mod.Id.ToString(), modOther.Name);

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

        mod.ToString().Should().Be("Mod<EId:200000000000002>");

        await VerifyTable(mod);
    }

    [Fact]
    public async Task CanPutEntitiesInDifferentPartitions()
    {

        using var tx = Connection.BeginTransaction();
        var file1 = new File.New(tx)
        {
            Path = "C:\\test1.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        var file2 = new File.New(tx, PartitionId.From(10))
        {
            Path = "C:\\test2.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        var file3 = new File.New(tx, PartitionId.From(200))
        {
            Path = "C:\\test3.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        // TempIds store the desired partition in the third highest byte
        (file1.Id.Value >> 40 & 0xFF).Should().Be(PartitionId.Entity.Value);
        (file2.Id.Value >> 40 & 0xFF).Should().Be(10);
        (file3.Id.Value >> 40 & 0xFF).Should().Be(200);

        var result = await tx.Commit();
        var file1RO = result.Remap(file1);
        var file2RO = result.Remap(file2);
        var file3RO = result.Remap(file3);


        var allDatoms = file1RO.Concat(file2RO).Concat(file3RO);

        await VerifyTable(allDatoms);
    }

    [Fact]
    public async Task CanLoadEntitiesWithoutSubclass()
    {
        var loadout = await InsertExampleData();

        var entityLoadout = new ReadOnlyModel(Connection.Db, loadout.Id);

        entityLoadout
            .Should().BeEquivalentTo(loadout);
    }

    [Fact]
    public async Task CanCreateTempEntities()
    {
        var loadoutOther = new TempEntity
        {
            { Loadout.Name, "Loadout Other" }
        };

        var loadout = new TempEntity
        {
            { Loadout.Name, "Test Loadout" },
            { Mod.LoadoutId, loadoutOther},
        };

        using var tx = Connection.BeginTransaction();
        loadout.AddTo(tx);
        var result = await tx.Commit();

        var loaded = Loadout.Load(result.Db, result[loadout.Id!.Value]);
        loaded.Name.Should().Be("Test Loadout");

        loadout.GetFirst(Loadout.Name).Should().Be("Test Loadout");

        Mod.LoadoutId.Get(loaded).Should().Be(result[loadoutOther.Id!.Value], "Sub entity should be added to the transaction");
    }

    [Fact]
    public async Task CanWorkWithMarkerAttributes()
    {
        var mod = new TempEntity
        {
            { Mod.Name, "Test Mod" },
            Mod.Marked,
        };

        using var tx = Connection.BeginTransaction();
        mod.AddTo(tx);
        var result = await tx.Commit();

        var reloaded = Mod.Load(result.Db, result[mod.Id!.Value]);
        reloaded.IsMarked.Should().BeTrue();

    }

    [Fact]
    public async Task CanExecuteTxFunctions()
    {
        EntityId id;
        // Create a loadout with inital state
        using var tx = Connection.BeginTransaction();
        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout: 1"
        };
        var result = await tx.Commit();
        id = result[loadout.Id];

        // Update it 1000 times in "parallel". The actual function is executed serially, but we queue up the updates
        // in parallel. If this was executed in parallel, we'd see a result other than 1001 at the end due to race conditions
        List<Task> tasks = [];
        {
            for (var i = 0; i < 1000; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var txInner = Connection.BeginTransaction();
                    // Send the function for the update, not update itself
                    txInner.Add(id, 1, AddToName);
                    await txInner.Commit();
                }));
            }
        }

        await Task.WhenAll(tasks);

        var db = Connection.Db;
        var loadoutRO = Loadout.Load(db, id);
        loadoutRO.Name.Should().Be("Test Loadout: 1001");

        return;

        // Actual work is done here, we load the entity and update it this is executed serially
        // by the transaction executor
        void AddToName(ITransaction tx, IDb db, EntityId eid, int amount)
        {
            var loadout = Loadout.Load(db, eid);
            var oldAmount = int.Parse(loadout.Name.Split(":")[1].Trim());
            tx.Add(loadout.Id, Loadout.Name, $"Test Loadout: {(oldAmount + amount)}");
        }
    }

    [Fact]
    public async Task NonRecursiveDeleteDeletesOnlyOneEntity()
    {
        var loadout = await InsertExampleData();
        var firstDb = Connection.Db;

        var firstMod = loadout.Mods.First();
        var firstFiles = firstMod.Files.ToArray();

        loadout.Mods.Count.Should().Be(3);

        using var tx = Connection.BeginTransaction();
        tx.Delete(firstMod.Id, false);
        var result = await tx.Commit();

        loadout = loadout.Rebase();

        loadout.Mods.Count.Should().Be(2);

        var modRefreshed = Mod.Load(result.Db, firstMod.ModId);
        modRefreshed.IsValid().Should().BeFalse("Mod should be deleted");

        Mod.TryGet(result.Db, firstMod.ModId, out _).Should().BeFalse("Mod should be deleted");
        Mod.TryGet(firstDb, firstMod.ModId, out _).Should().BeTrue("The history of the mod still exists");

        foreach (var file in firstFiles)
        {
            var reloaded = File.Load(result.Db, result[file.Id]);
            reloaded.IsValid().Should().BeTrue("File should still exist, the delete wasn't recursive");
        }
    }

    [Fact]
    public async Task RecursiveDeleteDeletesModsAsWellButNotCollections()
    {
        var loadout = await InsertExampleData();
        var firstDb = Connection.Db;
        var firstMod = loadout.Mods.First();

        using var extraTx = Connection.BeginTransaction();
        var collection = new Collection.New(extraTx)
        {
            Name = "Test Collection",
            ModIds = [firstMod],
            LoadoutId = loadout
        };
        var result = await extraTx.Commit();

        loadout = loadout.Rebase(result.Db);


        var firstFiles = firstMod.Files.ToArray();

        loadout.Mods.Count.Should().Be(3);
        loadout.Collections.Count.Should().Be(1);

        using var tx = Connection.BeginTransaction();
        tx.Delete(firstMod.Id, true);
        result = await tx.Commit();

        loadout = loadout.Rebase(result.Db);

        loadout.Mods.Count.Should().Be(2);
        loadout.Collections.Count.Should().Be(1);

        var modRefreshed = Mod.Load(result.Db, firstMod.ModId);
        modRefreshed.IsValid().Should().BeFalse("Mod should be deleted");

        Mod.TryGet(result.Db, firstMod.ModId, out _).Should().BeFalse("Mod should be deleted");
        Mod.TryGet(firstDb, firstMod.ModId, out _).Should().BeTrue("The history of the mod still exists");

        foreach (var file in firstFiles)
        {
            var reloaded = File.Load(result.Db, result[file.Id]);
            reloaded.IsValid().Should().BeFalse("File should be deleted, the delete was recursive");
        }
    }

    [Fact]
    public async Task CanReadAndWriteOptionalAttributes()
    {
        var loadout = await InsertExampleData();

        var firstMod = loadout.Mods.First();

        firstMod.Contains(Mod.Description).Should().BeFalse();


        using var tx = Connection.BeginTransaction();
        var mod = new Mod.New(tx)
        {
            LoadoutId = loadout,
            Name = "Test Mod",
            Source = new Uri("http://test.com"),
            Description = "Test Description"
        };
        var result = await tx.Commit();

        var remapped = result.Remap(mod);
        remapped.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task CanGetModelRevisions()
    {
        var loadout = await InsertExampleData();

        var loadoutNames = new List<string>();

        using var subscription = loadout.Revisions()
            .Select(l => l.Name)
            .Finally(() => loadoutNames.Add("DONE"))
            .Subscribe(l => loadoutNames.Add(l));


        loadoutNames.Count.Should().Be(1, "Only the current revision should be loaded");

        using var tx1 = Connection.BeginTransaction();
        tx1.Add(loadout.Id, Loadout.Name, "Update 1");
        var result = await tx1.Commit();

        using var tx2 = Connection.BeginTransaction();
        tx2.Add(loadout.Id, Loadout.Name, "Update 2");
        var result2 = await tx2.Commit();

        using var tx3 = Connection.BeginTransaction();
        tx3.Delete(loadout.Id, true);
        var result3 = await tx3.Commit();

        loadoutNames.Count.Should().Be(4, "All revisions should be loaded");

        loadoutNames.Should().BeEquivalentTo(["Test Loadout", "Update 1", "Update 2", "DONE"]);

    }

    [Fact]
    public async Task CanFindByReference()
    {
        var loadout = await InsertExampleData();
        foreach (var mod in loadout.Mods)
        {
            var found = Mod.FindByLoadout(Connection.Db, mod.LoadoutId)
                .Select(f => f.Id);
            found.Should().Contain(mod.Id, "we can look entities via the value if they are references");
        }
    }

    [Fact]
    public async Task CanObserveIndexChanges()
    {
        var loadout = await InsertExampleData();

        List<string[]> changes = new();

        // Define the slice to observe
        var slice = SliceDescriptor.Create(Mod.Name, Connection.Db.Registry);

        // Setup the subscription
        using var _ = ObservableDatoms.ObserveDatoms(Connection, slice)
            // Snapshot the values each time
            .QueryWhenChanged(datoms => datoms.Select(d => d.Resolved.ObjectValue.ToString()!).ToArray())
            // Add the changes to the list
            .Subscribe(x => changes.Add(x));

        // Rename a mod
        using var tx = Connection.BeginTransaction();
        tx.Add(loadout.Mods.First().Id, Mod.Name, "Test Mod 1");
        await tx.Commit();

        // Add a new mod
        using var tx2 = Connection.BeginTransaction();
        tx2.Add(tx2.TempId(), Mod.Name, "Test Mod 2");
        await tx2.Commit();

        // Delete the first mod
        using var tx3 = Connection.BeginTransaction();
        tx3.Retract(loadout.Mods.First().Id, Mod.Name, "Test Mod 1");
        await tx3.Commit();

        await Verify(changes);
    }

    /// <summary>
    /// Test for a flickering bug in UI users, where datoms that are changed result in a `add` and a `remove` operation
    /// causing the UI to flicker, instead we want to issue changes on a ScalarAttribute as a refresh/change
    /// </summary>
    [Fact]
    public async Task CanObserveIndexChangesWithoutFlickering()
    {
        
        var loadout = await InsertExampleData();
        
        List<IChangeSet<Datom>> changes = new();

        // Define the slice to observe
        var slice = SliceDescriptor.Create(Mod.Name, Connection.Db.Registry);

        // Setup the subscription
        using var _ = Connection.ObserveDatoms(slice)
            // Add the changes to the list
            .Subscribe(x => changes.Add(x));

        // Rename a mod, should result in a refresh, not a add and remove
        using var tx = Connection.BeginTransaction();
        tx.Add(loadout.Mods.First().Id, Mod.Name, "Test Mod 1");
        await tx.Commit();

        // Add a new mod
        using var tx2 = Connection.BeginTransaction();
        tx2.Add(tx2.TempId(), Mod.Name, "Test Mod 2");
        await tx2.Commit();

        // Delete the first mod
        using var tx3 = Connection.BeginTransaction();
        tx3.Retract(loadout.Mods.First().Id, Mod.Name, "Test Mod 1");
        await tx3.Commit();


        var changesProcessed = changes.Select(change => new
        {
            Added = change.Adds,
            Removed = change.Removes,
            Refreshes = change.Refreshes
        });
        
        await Verify(changesProcessed);
        
        
    }

    [Fact]
    public async Task MultipleIncludesDontSplitEntities()
    {
        using var tx = Connection.BeginTransaction();
        var child = new Child.New(tx, out var id)
        {
            Name = "Test Child",
            ParentA = new ParentA.New(tx, id)
            {
                Name = "Parent A"
            },
            ParentB = new ParentB.New(tx, id)
            {
                Name = "Parent B"
            }
        };
        
        var result = await tx.Commit();
        
        var childRO = result.Remap(child);
        
        childRO.AsParentA().Name.Should().Be("Parent A");
        childRO.AsParentB().Name.Should().Be("Parent B");
        childRO.Name.Should().Be("Test Child");
        

        // If the above is working correctly we'll only have one entityId for the client, if it's wrong, the
        // one of the parents may have a different entityId
        await VerifyTable(result.Db.Datoms(result.NewTx).Resolved());
    }
    
    [Fact]
    public async Task MultipleIncludesCanBeConstructedSeparately()
    {
        using var tx = Connection.BeginTransaction();
        
        var parentA = new ParentA.New(tx)
        {
            Name = "Parent A"
        };
        
        var child = new Child.New(tx, parentA.Id)
        {
            Name = "Test Child",
            ParentA = parentA,
            ParentB = new ParentB.New(tx, parentA.Id)
            {
                Name = "Parent B"
            }
        };
        
        var result = await tx.Commit();
        
        var childRO = result.Remap(child);
        
        childRO.AsParentA().Name.Should().Be("Parent A");
        childRO.AsParentB().Name.Should().Be("Parent B");
        childRO.Name.Should().Be("Test Child");
        

        // If the above is working correctly we'll only have one entityId for the client, if it's wrong, the
        // one of the parents may have a different entityId
        await VerifyTable(result.Db.Datoms(result.NewTx).Resolved());
    }
}
