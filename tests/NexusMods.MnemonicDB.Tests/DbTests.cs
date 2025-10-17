using System.Buffers;
using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.TestModel.Analyzers;
using NexusMods.Paths;
using TUnit.Assertions.AssertConditions.Throws;
using File = NexusMods.MnemonicDB.TestModel.File;
namespace NexusMods.MnemonicDB.Tests;

[WithServiceProvider]
public class DbTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    [Test]
    public async Task ReadDatomsForEntity()
    {
        const int totalCount = 10;
        var tx = Connection.BeginTransaction();


        var ids = new List<EntityId>();
        for (ulong idx = 0; idx < totalCount; idx++)
        {
            var file = new File.New(tx)
            {
                Path = $"test_{idx}.txt",
                Hash = Hash.From(idx + 0xDEADBEEF),
                Size = Size.From(idx),
                ModId = EntityId.From(1)
            };
            ids.Add(file.Id);
        }

        var oldTx = Connection.TxId;
        var result = await tx.Commit();

        await Assert.That(result.NewTx).IsNotEqualTo(oldTx).Because("transaction id should be incremented");
        await Assert.That(result.NewTx.Value).IsEqualTo(oldTx.Value + 1).Because("transaction id should be incremented by 1");

        var db = Connection.Db;
        var resolved = File.Load(db, ids.Select(id => result[id]));
        await VerifyModel(resolved);
    }

    [Test]
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

        var idx = 0;
        foreach (var txId in txEs)
        {
            var db = Connection.AsOf(txId);
            var resolved = db[modId].Resolved(Connection);
            await VerifyTable(resolved).UseTextForParameters("mod data_" + txId.Value);

            // Make sure we can still look up mods by indexed attributes
            if (idx > 0)
            {
                await Assert.That(Mod.FindByName(db, $"Test Mod {idx - 1}").Select(v => v.ModId.Value)).Contains(modId);
            }
            else
            {
                await Assert.That(Mod.FindByName(db, $"Test Mod").Select(v => v.ModId.Value)).Contains(modId);
            }

            idx += 1;
        }
    }


    [Test]
    public async Task DbIsImmutable()
    {
        const int times = 3;

        // Insert some data
        var tx = Connection.BeginTransaction();

        var file = new File.New(tx)
        {
            Path = "test.txt",
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
            newTx.Add(realId, File.Path, $"test_{i}.txt_mutate");

            await newTx.Commit();

            // Validate the data
            var newDb = Connection.Db;
                            await Assert.That(newDb.BasisTxId.Value).IsEqualTo(originalDb.BasisTxId.Value + 1UL + (ulong)i).Because("transaction id should be incremented by 1 for each mutation at iteration " + i);

            var newFound = File.Load(newDb, realId);
            await VerifyModel(newFound).UseTextForParameters("mutated data " + i);

            // Validate the original data
            var orignalFound = File.Load(originalDb, realId);
            await VerifyModel(orignalFound).UseTextForParameters("original data" + i);
        }
    }


    [Test]
    public async Task ReadModelsCanHaveExtraAttributes()
    {
        // Insert some data
        var tx = Connection.BeginTransaction();

        var archiveFile = new ArchiveFile.New(tx, out var id)
        {
            File = new File.New(tx, id)
            {
                Path = "test.txt",
                Hash = Hash.From(1 + 0xDEADBEEF),
                Size = Size.From(1),
                ModId = EntityId.From(1)
            },
            Path = "test.zip",
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

        await Assert.That(readModel.Id).IsEqualTo(archiveReadModel.Id).Because("both models are the same entity");

        await Assert.That(archiveReadModel.AsFile().ToArray()).IsEquivalentTo(readModel.ToArray()).Because("archive file should have the same base data as the file");

        await Assert.That(readModel.TryGetAsArchiveFile(out var castedDown)).IsTrue();
        
#pragma warning disable CS0183 // 'is' expression's given expression is always of the provided type
        await Assert.That(castedDown is ArchiveFile.ReadOnly).IsTrue();
#pragma warning restore CS0183 // 'is' expression's given expression is always of the provided type

        var badCast = new File.ReadOnly(result.Db, EntityId.From(1));
        await Assert.That(badCast.IsValid()).IsFalse().Because("bad cast should not validate");
        await Assert.That(badCast.TryGetAsArchiveFile(out var archiveFileBad)).IsFalse().Because("bad cast should not be able to cast down");
        await Assert.That(archiveFileBad.IsValid()).IsFalse().Because("bad cast should not validate as archive file");

        await Assert.That(castedDown).IsEquivalentTo(archiveReadModel).Because("casted down model should be the same as the original model");
    }

    [Test]
    public async Task CanGetCommitUpdates()
    {
        List<ResolvedDatom[]> updates = new();


        var tx = Connection.BeginTransaction();
        var file = new File.New(tx)
        {
            Path = "test.txt",
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
                updates.Add(update.RecentlyAdded.Resolved(Connection).ToArray());
        });

        for (var idx = 0; idx < 4; idx++)
        {
            tx = Connection.BeginTransaction();
            tx.Add(realId, File.Hash, Hash.From(0xDEADBEEF + (ulong)idx + 0xEE));
            result = await tx.Commit();

            await Task.Delay(100);

            // +2 because we always get one update for the initial state and one for the new state
            await Assert.That(updates).HasCount(idx + 2);
            var updateDatom = updates[idx + 1];

            await VerifyTable(updateDatom)
                .UseTextForParameters("update_datom_" + idx);
        }
    }

    [Test]
    public async Task TimestampsArentBorked()
    {
        using var tx = Connection.BeginTransaction();
        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout"
        };
        
        var result = await tx.Commit();
        
        var recentTimestamp = result.Db.RecentlyAdded
            .Resolved(Connection)
            .First(d => d.A == Transaction.Timestamp);
        
        await Assert.That(recentTimestamp.ObjectValue).IsAssignableTo<DateTimeOffset>();
                    await Assert.That((DateTimeOffset)recentTimestamp.ObjectValue)
            .IsAfter(DateTimeOffset.UtcNow.AddSeconds(-100))
            .And.IsBefore(DateTimeOffset.UtcNow.AddSeconds(100));
    }

    [Test]
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

        await Assert.That(loadoutWritten.Mods.ToArray()).HasCount(2);
        await Assert.That(loadoutWritten.Mods.Select(m => m.Name)).IsEquivalentTo(["Test Mod 1", "Test Mod 2"]);

        var firstMod = loadoutWritten.Mods.First();
        await Assert.That(firstMod.Loadout.Id.InPartition(PartitionId.Entity)).IsTrue().Because("LoadoutId should in the entity partition");
        await Assert.That(firstMod.LoadoutId).IsEquivalentTo(loadoutWritten.LoadoutId);
        await Assert.That(firstMod.Db).IsEqualTo(newDb);
        await Assert.That(loadout.Name).IsEqualTo("Test Loadout");
        await Assert.That(firstMod.Loadout.Name).IsEqualTo("Test Loadout");
    }


    [Test]
    public async Task CanFindEntitiesByAttribute()
    {
        var table = TableResults();
        await InsertExampleData();

        var db = Connection.Db;

        var ids = from mod in Mod.All(db)
                  from modOther in Mod.FindByName(db, mod.Name)
                  select (mod.Name, mod.Id.ToString(), modOther.Name);

        await Verify(ids);
    }

    [Test]
    public async Task CanGetDatomsFromEntity()
    {
        var loadout = await InsertExampleData();
        var mod = loadout.Mods.First();

        await Assert.That(mod.Contains(Mod.Name)).IsTrue();
        await Assert.That(mod.Contains(Mod.Source)).IsTrue();
        await Assert.That(mod.Contains(Loadout.Name)).IsFalse();

        await Assert.That(mod.ToString()).IsEqualTo("Mod<EId:200000000000002>");

        await VerifyTable(mod);
    }

    [Test]
    public async Task CanPutEntitiesInDifferentPartitions()
    {

        using var tx = Connection.BeginTransaction();
        var file1 = new File.New(tx)
        {
            Path = "test1.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        var file2 = new File.New(tx, tx.TempId(PartitionId.From(10)))
        {
            Path = "test2.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        var file3 = new File.New(tx, tx.TempId(PartitionId.From(200)))
        {
            Path = "test3.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = EntityId.From(1)
        };

        // TempIds store the desired partition in the third highest byte
        await Assert.That(file1.Id.Value >> 40 & 0xFF).IsEqualTo(PartitionId.Entity.Value);
        await Assert.That((int)(file2.Id.Value >> 40 & 0xFF)).IsEqualTo(10);
        await Assert.That((int)(file3.Id.Value >> 40 & 0xFF)).IsEqualTo(200);

        var result = await tx.Commit();
        var file1RO = result.Remap(file1);
        var file2RO = result.Remap(file2);
        var file3RO = result.Remap(file3);


        var allDatoms = file1RO.Concat(file2RO).Concat(file3RO);

        await VerifyTable(allDatoms);
    }

    [Test]
    public async Task CanLoadEntitiesWithoutSubclass()
    {
        var loadout = await InsertExampleData();

        var entityLoadout = new ReadOnlyModel(Connection.Db, loadout.Id);

                    await Assert.That(entityLoadout).IsEquivalentTo(loadout);
    }
    

    [Test]
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
                    txInner.AddTxFn((datoms, db) =>
                    {
                        // Actual work is done here, we load the entity and update it this is executed serially
                        // by the transaction executor
                        var loadout = Loadout.Load(db, id);
                        var oldAmount = int.Parse(loadout.Name.Split(":")[1].Trim());
                        datoms.Add(loadout.Id, Loadout.Name, $"Test Loadout: {(oldAmount + i)}");
                    });
                    await txInner.Commit();
                }));
            }
        }

        await Task.WhenAll(tasks);

        var db = Connection.Db;
        var loadoutRO = Loadout.Load(db, id);
        await Assert.That(loadoutRO.Name).IsEqualTo("Test Loadout: 1001");

        return;


        void AddToName(ATransaction tx, IDb db, EntityId eid, int amount)
        {

        }
    }

    [Test]
    public async Task NonRecursiveDeleteDeletesOnlyOneEntity()
    {
        var loadout = await InsertExampleData();
        var firstDb = Connection.Db;

        var firstMod = loadout.Mods.First();
        var firstFiles = firstMod.Files.ToArray();

        await Assert.That(loadout.Mods.Count).IsEqualTo(3);

        using var tx = Connection.BeginTransaction();
        tx.Delete(firstMod.Id, false);
        var result = await tx.Commit();

        loadout = loadout.Rebase();

        await Assert.That(loadout.Mods.Count).IsEqualTo(2);

        var modRefreshed = Mod.Load(result.Db, firstMod.ModId);
        await Assert.That(modRefreshed.IsValid()).IsFalse().Because("Mod should be deleted");

        await Assert.That(Mod.TryGet(result.Db, firstMod.ModId, out _)).IsFalse().Because("Mod should be deleted");
        await Assert.That(Mod.TryGet(firstDb, firstMod.ModId, out _)).IsTrue().Because("The history of the mod still exists");

        foreach (var file in firstFiles)
        {
            var reloaded = File.Load(result.Db, result[file.Id]);
            await Assert.That(reloaded.IsValid()).IsTrue().Because("File should still exist, the delete wasn't recursive");
        }
    }
    [Test]
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

        await Assert.That(loadout.Mods.ToArray()).HasCount(3);
        await Assert.That(loadout.Collections.Count).IsEqualTo(1);

        using var tx = Connection.BeginTransaction();
        tx.Delete(firstMod.Id, true);
        result = await tx.Commit();

        loadout = loadout.Rebase(result.Db);

        await Assert.That(loadout.Mods.Count).IsEqualTo(2);
        await Assert.That(loadout.Collections.Count).IsEqualTo(1);

        var modRefreshed = Mod.Load(result.Db, firstMod.ModId);
        await Assert.That(modRefreshed.IsValid()).IsFalse().Because("Mod should be deleted");

        await Assert.That(Mod.TryGet(result.Db, firstMod.ModId, out _)).IsFalse().Because("Mod should be deleted");
        await Assert.That(Mod.TryGet(firstDb, firstMod.ModId, out _)).IsTrue().Because("The history of the mod still exists");

        foreach (var file in firstFiles)
        {
            var reloaded = File.Load(result.Db, result[file.Id]);
            await Assert.That(reloaded.IsValid()).IsFalse().Because("File should be deleted, the delete was recursive");
        }
    }

    [Test]
    public async Task CanReadAndWriteOptionalAttributes()
    {
        var loadout = await InsertExampleData();

        var firstMod = loadout.Mods.First();

        await Assert.That(firstMod.Contains(Mod.Description)).IsFalse();


        using var tx = Connection.BeginTransaction();
        var modWithDescription = new Mod.New(tx)
        {
            LoadoutId = loadout,
            Name = "Test Mod",
            Source = new Uri("http://test.com"),
            Description = "Test Description"
        };
        
        var modWithoutDiscription = new Mod.New(tx)
        {
            LoadoutId = loadout,
            Name = "Test Mod 2",
            Source = new Uri("http://test.com"),
        };
        
        var result = await tx.Commit();

        var remapped = result.Remap(modWithDescription);
        await Assert.That(remapped.Contains(Mod.Description)).IsTrue();
        await Assert.That(Mod.Description.TryGetValue(remapped.EntitySegment, out var foundDesc)).IsTrue();
        await Assert.That(foundDesc).IsEqualTo("Test Description");
        await Assert.That(remapped.Description.Value).IsEqualTo("Test Description");

        var remapped2 = result.Remap(modWithoutDiscription);
        await Assert.That(remapped2.Contains(Mod.Description)).IsFalse();
        await Assert.That(Mod.Description.TryGetValue(remapped2.EntitySegment, out var foundDesc2)).IsFalse();
    }

    [Test]
    public async Task CanGetModelRevisions()
    {
        var loadout = await InsertExampleData();

        var loadoutNames = new List<string>();

        using var subscription = loadout.Revisions()
            .Select(l => l.Name)
            .Finally(() => loadoutNames.Add("DONE"))
            .Subscribe(l => loadoutNames.Add(l));

        // Delay just a tad to make sure the initial subscription goes through
        await Task.Delay(100);

        await Assert.That(loadoutNames).HasCount(1).Because("Only the current revision should be loaded");

        using var tx1 = Connection.BeginTransaction();
        tx1.Add(loadout.Id, Loadout.Name, "Update 1");
        var result = await tx1.Commit();

        using var tx2 = Connection.BeginTransaction();
        tx2.Add(loadout.Id, Loadout.Name, "Update 2");
        var result2 = await tx2.Commit();

        using var tx3 = Connection.BeginTransaction();
        tx3.Delete(loadout.Id, true);
        var result3 = await tx3.Commit();

        await Assert.That(loadoutNames).HasCount(4).Because("All revisions should be loaded");

        await Assert.That(loadoutNames).IsEquivalentTo(["Test Loadout", "Update 1", "Update 2", "DONE"]);

    }

    [Test]
    public async Task CanFindByReference()
    {
        var loadout = await InsertExampleData();
        foreach (var mod in loadout.Mods)
        {
            var found = Mod.FindByLoadout(Connection.Db, mod.LoadoutId)
                .Select(f => f.Id);
            await Assert.That(found).Contains(mod.Id).Because("we can look entities via the value if they are references");
        }
    }

    [Test]
    public async Task CanObserveIndexChanges()
    {
        var loadout = await InsertExampleData();

        List<string[]> changes = new();

        // Define the slice to observe
        var slice = SliceDescriptor.Create(Mod.Name, AttributeCache);

        var resolver = Connection.AttributeResolver;
        // Setup the subscription
        using var _ = Connection.ObserveDatoms(slice)
            // Snapshot the values each time
            .QueryWhenChanged(datoms => datoms.Items.Select(d => resolver.Resolve(d)).ToArray())
            // Add the changes to the list
            .Subscribe(x =>
            {
                var data = x.Select(o => o.ObjectValue.ToString()!).Order().ToArray();
                changes.Add(data);
            });

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

    [Test]
    public async Task ObserveDatomsEmitsUpdatesForScalarChanges()
    {
        var loadout = await InsertExampleData();
        var targetMod = loadout.Mods.First();
        var originalName = targetMod.Name;
        var newName = originalName + " (Renamed)";

        var slice = SliceDescriptor.Create(Mod.Name, AttributeCache);

        var tcs = new TaskCompletionSource<IChangeSet<Datom, DatomKey>>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = Connection.ObserveDatoms(slice)
            .Subscribe(changeSet =>
            {
                if (changeSet.Updates == 0)
                    return;

                tcs.TrySetResult(changeSet);
            });

        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(targetMod.Id, Mod.Name, newName);
            await tx.Commit();
        }

        var changeSet = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(changeSet.Adds).IsEqualTo(0).Because("scalar updates should not appear as adds");
        await Assert.That(changeSet.Removes).IsEqualTo(0).Because("scalar updates should not appear as removes");
        await Assert.That(changeSet.Updates).IsEqualTo(1).Because("the change should surface as a single update");

        var update = changeSet.Single(change => change.Reason == ChangeReason.Update);
        var resolver = Connection.AttributeResolver;

        var currentName = resolver.Resolve(update.Current).ObjectValue?.ToString();
        await Assert.That(currentName).IsEqualTo(newName);

        var previous = update.Previous;
        await Assert.That(previous.HasValue).IsTrue();

        var previousName = resolver.Resolve(previous.Value).ObjectValue?.ToString();
        await Assert.That(previousName).IsEqualTo(originalName);
    }

    [Test]
    public async Task ObserveDatomsEmitsAddsForCollectionAttributes()
    {
        var loadout = await InsertExampleData();
        var targetMod = loadout.Mods.First();
        const string newTag = "New Tag";

        var slice = SliceDescriptor.Create(Mod.Tags, AttributeCache);
        var resolver = Connection.AttributeResolver;

        var tcs = new TaskCompletionSource<IChangeSet<Datom, DatomKey>>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = Connection.ObserveDatoms(slice)
            .Subscribe(changeSet =>
            {
                foreach (var change in changeSet)
                {
                    if (change.Reason != ChangeReason.Add)
                        continue;

                    var value = resolver.Resolve(change.Current).ObjectValue?.ToString();
                    if (value == newTag)
                    {
                        tcs.TrySetResult(changeSet);
                        break;
                    }
                }
            });

        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(targetMod.Id, Mod.Tags, newTag);
            await tx.Commit();
        }

        var changeSet = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(changeSet.Adds).IsEqualTo(1).Because("collection additions should surface as adds");
        await Assert.That(changeSet.Removes).IsEqualTo(0);
        await Assert.That(changeSet.Updates).IsEqualTo(0);

        var addition = changeSet.Single(change => change.Reason == ChangeReason.Add);
        var addedValue = resolver.Resolve(addition.Current).ObjectValue?.ToString();
        await Assert.That(addedValue).IsEqualTo(newTag);
    }

    [Test]
    public async Task ObserveDatomsEmitsRemovesForCollectionAttributes()
    {
        var loadout = await InsertExampleData();
        var targetMod = loadout.Mods.First();
        const string tagToRemove = "To Remove";

        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(targetMod.Id, Mod.Tags, tagToRemove);
            await tx.Commit();
        }

        var slice = SliceDescriptor.Create(Mod.Tags, AttributeCache);
        var resolver = Connection.AttributeResolver;
        var tcs = new TaskCompletionSource<IChangeSet<Datom, DatomKey>>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = Connection.ObserveDatoms(slice)
            .Subscribe(changeSet =>
            {
                foreach (var change in changeSet)
                {
                    if (change.Reason != ChangeReason.Remove)
                        continue;

                    var value = resolver.Resolve(change.Current).ObjectValue?.ToString();
                    if (value == tagToRemove)
                    {
                        tcs.TrySetResult(changeSet);
                        break;
                    }
                }
            });

        using (var tx = Connection.BeginTransaction())
        {
            tx.Retract(targetMod.Id, Mod.Tags, tagToRemove);
            await tx.Commit();
        }

        var changeSet = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(changeSet.Adds).IsEqualTo(0);
        await Assert.That(changeSet.Removes).IsEqualTo(1).Because("collection removals should surface as removes");
        await Assert.That(changeSet.Updates).IsEqualTo(0);

        var removal = changeSet.Single(change => change.Reason == ChangeReason.Remove);
        var removedValue = resolver.Resolve(removal.Current).ObjectValue?.ToString();
        await Assert.That(removedValue).IsEqualTo(tagToRemove);
    }

    [Test]
    public async Task ObserveDatomsSeparatesScalarAddAndRemoveAcrossTransactions()
    {
        var loadout = await InsertExampleData();
        var targetMod = loadout.Mods.First();
        var originalName = targetMod.Name;
        var replacementName = originalName + " (Replacement)";

        var slice = SliceDescriptor.Create(Mod.Name, AttributeCache);
        var resolver = Connection.AttributeResolver;

        var removeTcs = new TaskCompletionSource<IChangeSet<Datom, DatomKey>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var addTcs = new TaskCompletionSource<IChangeSet<Datom, DatomKey>>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = Connection.ObserveDatoms(slice)
            .Subscribe(changeSet =>
            {
                foreach (var change in changeSet)
                {
                    var value = resolver.Resolve(change.Current).ObjectValue?.ToString();
                    switch (change.Reason)
                    {
                        case ChangeReason.Remove when value == originalName:
                            removeTcs.TrySetResult(changeSet);
                            break;
                        case ChangeReason.Add when value == replacementName:
                            addTcs.TrySetResult(changeSet);
                            break;
                    }
                }
            });

        using (var tx = Connection.BeginTransaction())
        {
            tx.Retract(targetMod.Id, Mod.Name, originalName);
            await tx.Commit();
        }

        var removeChanges = await removeTcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(targetMod.Id, Mod.Name, replacementName);
            await tx.Commit();
        }

        var addChanges = await addTcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(removeChanges.Adds).IsEqualTo(0);
        await Assert.That(removeChanges.Removes).IsEqualTo(1).Because("scalar retracts in their own transaction should surface as removes");
        await Assert.That(removeChanges.Updates).IsEqualTo(0);

        var removal = removeChanges.Single(change => change.Reason == ChangeReason.Remove);
        var removedName = resolver.Resolve(removal.Current).ObjectValue?.ToString();
        await Assert.That(removedName).IsEqualTo(originalName);

        await Assert.That(addChanges.Adds).IsEqualTo(1).Because("scalar asserts in their own transaction should surface as adds");
        await Assert.That(addChanges.Removes).IsEqualTo(0);
        await Assert.That(addChanges.Updates).IsEqualTo(0);

        var addition = addChanges.Single(change => change.Reason == ChangeReason.Add);
        var addedName = resolver.Resolve(addition.Current).ObjectValue?.ToString();
        await Assert.That(addedName).IsEqualTo(replacementName);
    }

    [Test]
    public async Task ObserveLargeDatomChanges()
    {
        var list = Connection.ObserveDatoms(Loadout.Name)
            .Select(f => f)
            .AsObservableCache();

        using var tx = Connection.BeginTransaction();
        for (var i = 0; i < 10000; i++)
        {
            _ = new Loadout.New(tx)
            {
                Name = $"Test Loadout {i}"
            };
        }

        var sw = Stopwatch.StartNew();
        await tx.Commit();

        var allLoadouts = Loadout.All(Connection.Db).Count;
        await Assert.That(sw.ElapsedMilliseconds).IsLessThan(5000).Because("the ObserveDatoms algorithm isn't stupidly slow");

        await Assert.That(list.Items).HasCount(10000);
        
        
        using var tx2 = Connection.BeginTransaction();

        var loadout = list.Items.Skip(10).First();
        
        tx2.Add(loadout.E, Loadout.Name, "Test Loadout 10 Updated");
        await tx2.Commit();
        
        await Assert.That(list.Items.First(datom => datom.E == loadout.E).Resolved(Connection.AttributeResolver).ObjectValue).IsEqualTo("Test Loadout 10 Updated");
        await Assert.That(allLoadouts).IsEqualTo(10000);
    }

    [Test]
    public async Task CanNestObserveDatoms()
    {
        // To test nesting of observables, we're going to observe all loadouts, and then inside that observe all mods

        List<EntityId> modIds = new();

        using var disposable = Connection.ObserveDatoms(Loadout.Name)
            .QueryWhenChanged(loadouts =>
            {
                foreach (var loadout in loadouts.Items)
                {
                    var mods = Connection.ObserveDatoms(Mod.Loadout, loadout.E)
                        .QueryWhenChanged(mods =>
                        {
                            modIds.AddRange(mods.Items.Select(m => m.E));
                            return 0;
                        })
                        .Subscribe();
                }
                return 42;
            })
            .Subscribe();
        
        using var tx = Connection.BeginTransaction();
        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout"
        };
        
        for (var i = 0; i < 10; i++)
        {
            _ = new Mod.New(tx)
            {
                Name = $"Test Mod {i}",
                Source = new Uri("http://test.com"),
                LoadoutId = loadout
            };
        }
        
        await tx.Commit();
        
        // Delay because the chain of observables is async
        await Task.Delay(1000);
        
        await Assert.That(modIds.Count).IsEqualTo(10);


    }
    
    [Test]
    public async Task CanGetInitialDbStateFromObservable()
    {
        var attrs = await Connection.ObserveDatoms(AttributeDefinition.UniqueId).FirstAsync();
        await Assert.That(attrs.Adds).IsGreaterThan(0);

        
        attrs = await Connection.ObserveDatoms(AttributeDefinition.UniqueId).FirstAsync();
        await Assert.That(attrs.Adds).IsGreaterThan(0);

    }

    /// <summary>
    /// Test for a flickering bug in UI users, where datoms that are changed result in a `add` and a `remove` operation
    /// causing the UI to flicker, instead we want to issue changes on a ScalarAttribute as a replace operation.
    /// </summary>
    [Test]
    public async Task CanObserveIndexChangesWithoutFlickering()
    {
        
        var loadout = await InsertExampleData();
        
        List<IChangeSet<Datom, DatomKey>> changes = new();

        // Define the slice to observe
        var slice = SliceDescriptor.Create(Mod.Name, AttributeCache);

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
            Updates = change.Updates
        });
        
        await Verify(changesProcessed);
        
        
    }

    [Test]
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
        
        await Assert.That(childRO.AsParentA().Name).IsEqualTo("Parent A");
        await Assert.That(childRO.AsParentB().Name).IsEqualTo("Parent B"); 
        await Assert.That(childRO.Name).IsEqualTo("Test Child");

        // If the above is working correctly we'll only have one entityId for the client, if it's wrong, the
        // one of the parents may have a different entityId
        await VerifyTable(result.Db.Datoms(result.NewTx).Resolved(Connection));
    }
    [Test]
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
        await Assert.That(childRO.AsParentA().Name).IsEqualTo("Parent A");
        await Assert.That(childRO.AsParentB().Name).IsEqualTo("Parent B");
        await Assert.That(childRO.Name).IsEqualTo("Test Child");

        // If the above is working correctly we'll only have one entityId for the client, if it's wrong, the
        // one of the parents may have a different entityId
        await VerifyTable(result.Db.Datoms(result.NewTx).Resolved(Connection));
    }

    [Test]
    public async Task CanGetAnalyzerData()
    {
        using var tx = Connection.BeginTransaction();
        
        var loadout1 = new Loadout.New(tx)
        {
            Name = "Test Loadout"
        };
        
        var mod = new Mod.New(tx)
        {
            Name = "Test Mod",
            Source = new Uri("http://test.com"),
            LoadoutId = loadout1 
        };

        var result = await tx.Commit();

        await Assert.That(result.Db).IsEqualTo(Connection.Db);
        
        var countData = Connection.Db.AnalyzerData<DatomCountAnalyzer, int>();
        await Assert.That(countData).IsEqualTo(result.Db.RecentlyAdded.Count);

        var attrs = Connection.Db.AnalyzerData<AttributesAnalyzer, HashSet<Symbol>>();
        await Assert.That(attrs).IsNotEmpty();
    }

    [Test]
    public async Task CanGetAttributesThatRequireDI()
    {
        var fileSystem = Provider.GetRequiredService<IFileSystem>();
        
        var path = fileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("foo/bar/qux.txt");
        
        using var tx = Connection.BeginTransaction();
        
        var loadout1 = new Loadout.New(tx)
        {
            Name = "Test Loadout",
            GamePath = path
        };

        var result = await tx.Commit();
        var loadout = result.Remap(loadout1);
        await Assert.That(loadout.GamePath.Value).IsEqualTo(path);
    }

    [Test]
    public async Task CollectionAttributesAreSupportedOnModels()
    {
        using var tx = Connection.BeginTransaction();
        
        var loadout1 = new Loadout.New(tx)
        {
            Name = "Test Loadout"
        };
        
        var mod = new Mod.New(tx)
        {
            Name = "Test Mod",
            Source = new Uri("http://test.com"),
            LoadoutId = loadout1,
            Tags = ["A", "B", "C"]
        };

        var result = await tx.Commit();
        
        var modRO = result.Remap(mod);
        
        await Assert.That(modRO.Tags).IsEquivalentTo(["A", "B", "C"]);
    }

    /// <summary>
    /// Tests a bug experienced in the app, when RefCount is used with ObserveDatoms, if the last subscription
    /// is disposed, and then another subscriber attaches, an exception would be thrown. This test ensures that
    /// this behavior works correctly.
    /// </summary>
    [Test]
    public async Task RefCountWorksWithObservables()
    {
        var tx = Connection.BeginTransaction();
        var mod = new Mod.New(tx)
        {
            Name = "Test Mod",
            Source = new Uri("http://test.com"),
            LoadoutId = EntityId.From(0)
        };
        var result = await tx.Commit();
        
        var modRO = result.Remap(mod);

        var refObs = Mod.Observe(Connection, modRO.Id)
            .Replay(1)
            .RefCount();
        
        List<Mod.ReadOnly> mods = [];
        
        var firstDisp = refObs.Subscribe(mods.Add);

        {
            var tx2 = Connection.BeginTransaction();
            tx2.Add(modRO.Id, Mod.Name, "Test Mod 2");
            await tx2.Commit();
        }
        
        firstDisp.Dispose();
        
        using var secondDisp = refObs.Subscribe(mods.Add);
        {
            var tx2 = Connection.BeginTransaction();
            tx2.Add(modRO.Id, Mod.Name, "Test Mod 3");
            await tx2.Commit();
        }
        
        // Distinct, because re-subscribing to the same observable will result in the same object being added
        // when it set the initial value
        await Verify(mods.Select(m => m.Name).Distinct());
    }
    
    [Test]
    public async Task CanExciseEntities()
    {
        using var tx = Connection.BeginTransaction();
        var l1 = new Loadout.New(tx)
        {
            Name = "Test Loadout 1"
        };
        
        var l2 = new Loadout.New(tx)
        {
            Name = "Test Loadout 2"
        };
        
        var results = await tx.Commit();
        
        var l1RO = results.Remap(l1);
        var l2RO = results.Remap(l2);

        {
            using var tx2 = Connection.BeginTransaction();
            tx2.Add(l2RO, Loadout.Name, "Test Loadout 2 Updated");
            await tx2.Commit();
        }
        l2RO = l2RO.Rebase();
        
        await Assert.That(l1RO.Name).IsEqualTo("Test Loadout 1");
        await Assert.That(l2RO.Name).IsEqualTo("Test Loadout 2 Updated");


        var history = Connection.History();

        await Assert.That(history[l2RO.Id]
            .Resolved(Connection)
            .OfType<StringAttribute.ResolvedDatom>()
            .Select(d => (!d.IsRetract, d.V)))
            .IsEquivalentTo([
                (true, "Test Loadout 2"),
                (false, "Test Loadout 2"),
                (true, "Test Loadout 2 Updated")
            ]);
        
        await Connection.Excise([l2RO.Id]);
        
        history = Connection.History();

        await Assert.That(history[l2RO.Id]).IsEmpty();

        await Assert.That(history[l1RO.Id]).IsNotEmpty();
    }

    [Test]
    public async Task CanHandleLargeNumbersOfSubscribers()
    {
        List<EntityId> mods = new();

        using var tx = Connection.BeginTransaction();

        // Create 10k mods
        for (var i = 0; i < 10000; i++)
        {
            var tmpMod = new Mod.New(tx)
            {
                Name = "Test Mod " + i,
                Source = new Uri("http://test.com"),
                LoadoutId = EntityId.From(0)
            };
            mods.Add(tmpMod.Id);
        }

        var result = await tx.Commit();

        // Update all the ids
        for (var i = 0; i < 10000; i++)
        {
            mods[i] = result[mods[i]];
        }

        List<IDisposable> subs = [];

        foreach (var id in mods)
        {
            subs.Add(Mod.Observe(Connection, id).Subscribe());
        }

        using var tx2 = Connection.BeginTransaction();

        // Add a lot of new datoms
        for (var i = 0; i < 10000; i++)
        {

            _ = new Mod.New(tx2)
            {
                Name = "Test Mod 10000",
                Source = new Uri("http://test.com"),
                LoadoutId = EntityId.From(0)
            };
        }


        var sw = Stopwatch.StartNew();
        await tx2.Commit();
        Logger.LogInformation("Time to commit: " + sw.ElapsedMilliseconds);
        
        await Assert.That(sw.Elapsed).IsLessThan(TimeSpan.FromSeconds(10)).Because("Should be able to handle a large number of non-overlapping subscribers");
        
        foreach (var sub in subs)
        {
            sub.Dispose();
        }
    }

    [Test]
    public async Task CanFlushAndCompactTheDB()
    {
        var tx = Connection.BeginTransaction();

        for (int i = 0; i < 1000; i++)
        {
            _ = new Mod.New(tx)
            {
                Name = "Test Mod " + i,
                Source = new Uri("http://test.com"),
                LoadoutId = EntityId.From(0)
            };
        }
        await tx.Commit();

        await Connection.FlushAndCompact();
    }

    [Test]
    public async Task CanPerformDatomScanUpdate()
    {
        var tx = Connection.BeginTransaction();
        
        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout"
        };

        _ = new Mod.New(tx)
        {
            Name = "Test Mod 1",
            Source = new Uri("http://test.com"),
            LoadoutId = loadout
        };
        
        _ = new Mod.New(tx)
        {
            Name = "Test Mod 2",
            Source = new Uri("http://test.com"),
            LoadoutId = loadout
        };
        
        _ = new Mod.New(tx)
        {
            Name = "Test Mod 3",
            Source = new Uri("http://test.com"),
            LoadoutId = loadout
        };

        var results = await tx.Commit();
        
        var loadoutRO = results.Remap(loadout);
        
        await Assert.That(loadoutRO.Mods.Select(m => m.Name).Order())
            .IsEquivalentTo(["Test Mod 1", "Test Mod 2", "Test Mod 3"]);

        var newResult = await Connection.ScanUpdate(UpdateFunc);

        loadoutRO = loadoutRO.Rebase(newResult.Db);
        
        await Assert.That(loadoutRO.Mods
            .Where(m => m.Contains(Mod.Name))
            .Select(m => m.Name).Order())
            .IsEquivalentTo(["Test Mod 1", "UPDATED Test Mod 3 !!"]);

        return;

        ScanResultType UpdateFunc(ref KeyPrefix prefix, ReadOnlySpan<byte> input, in IBufferWriter<byte> output)
        {
            if (prefix.ValueTag != ValueTag.Utf8)
                return ScanResultType.None;

            var value = Utf8Serializer.Read(input);
            switch (value)
            {
                case "Test Mod 1":
                    return ScanResultType.None;
                case "Test Mod 2":
                    return ScanResultType.Delete;
                case "Test Mod 3":
                    Utf8Serializer.Write("UPDATED Test Mod 3 !!", output);
                    return ScanResultType.Update;
                default:
                    return ScanResultType.None;
            }
        }

    }

    [Test]
    public async Task UniqueAttributesThrowExceptions()
    {
        var tmpId1 = PartitionId.Temp.MakeEntityId(0x42);
        var tmpId2 = PartitionId.Temp.MakeEntityId(0x43);
        
        // Two conflicting inserts
        using var tx1 = Connection.BeginTransaction();
        tx1.Add(tmpId1, ArchiveFile.Hash, Hash.From(0xDEADBEEF));
        tx1.Add(tmpId2, ArchiveFile.Hash, Hash.From(0xDEADBEEF));
        await Assert.That(async () => await tx1.Commit()).Throws<UniqueConstraintException>();
        
        // Two conflicts from different transactions
        using var tx2 = Connection.BeginTransaction();
        tx2.Add(tmpId1, ArchiveFile.Hash, Hash.From(0xDEADBEEF));
        var result = await tx2.Commit();
        var insertedId = result[tmpId1]; 
        
        // This should throw because a previous transaction inserted the same value
        using var tx3 = Connection.BeginTransaction();
        tx3.Add(tmpId2, ArchiveFile.Hash, Hash.From(0xDEADBEEF));
        await Assert.That(async () => await tx3.Commit()).Throws<UniqueConstraintException>();
        
        // Now let's retract the previous datom and set the datom again in the same transaction (out of order just to
        // make sure we can process that). This should not throw, because the datom is retracted for the other unique
        // constraint inside the same transaction that we are adding the new one.
        using var tx4 = Connection.BeginTransaction();
        tx4.Add(tmpId2, ArchiveFile.Hash, Hash.From(0xDEADBEEF));
        tx4.Retract(insertedId, ArchiveFile.Hash, Hash.From(0xDEADBEEF));
        await tx4.Commit();
    }
    
    
    //[Test]
    private async Task ObserverFuzzingTests()
    {
        using var tx = Connection.BeginTransaction();
        var file = new File.New(tx)
        {
            Path = "test.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(0),
            ModId = EntityId.From(1)
        };
        
        var result = await tx.Commit();
        var fileId = result[file.Id];
        
        
        var txTask = Task.Run(async () =>
        {
            for (var i = 0; i < 10_000; i++)
            {
                using var tx2 = Connection.BeginTransaction();
                tx2.Add(fileId, File.Size, Size.From((ulong)i));
                await tx2.Commit();
            }
        });

        List<Task<List<Size>>> tasks = new();
        CancellationTokenSource cts = new();

        for (int i = 0; i < 100; i++)
        {
            var obsTask = Task.Run(async () =>
            {
                List<Size> sizes = new();
                await Task.Delay(Random.Shared.Next(10, 2000), cts.Token);
                using var obs = File.Observe(Connection, fileId)
                    .Subscribe(file => sizes.Add(file.Size));
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), cts.Token);
                }
                catch (TaskCanceledException)
                {
                    // ignored
                }
                return sizes;
            });
            tasks.Add(obsTask);
        }

        await txTask;
        await cts.CancelAsync();
        await Task.WhenAll(tasks);

        int taskId = 0;
        foreach (var task in tasks)
        {
            var results = await task;
            await Assert.That(results.Count).IsGreaterThan(0);
            var prevResult = results.First();
            var idx = 0;
            foreach (var thisResult in results.Skip(1))
            {
                await Assert.That(thisResult.Value).IsEqualTo(prevResult.Value + 1).Because($"no updates should be dropped at index {idx} of {taskId} previous result was {prevResult.Value}");
                prevResult = thisResult;
                idx++;
            }
            taskId++;
        }
    }

    [Test]
    public async Task EntitiesCanStoreLongStrings()
    {
        using var tx = Connection.BeginTransaction();
        var loadout = new Loadout.New(tx)
        {
            Name = new string('A', 10000)
        };
        
        var result = await tx.Commit();
        
        var loadoutRO = result.Remap(loadout);
        
        await Assert.That(loadoutRO.Name).IsEqualTo(new string('A', 10000));
    }

    [Test]
    public async Task Test_NestedTransactions()
    {
        using var tx = Connection.BeginTransaction();

        EntityId loadout1Id;
        EntityId loadout2Id;
        using (var subTx1 = tx.CreateSubTransaction())
        {
            loadout1Id = new Loadout.New(subTx1)
            {
                Name = "Foo"
            };

            using (var subTx2 = subTx1.CreateSubTransaction())
            {
                loadout2Id = new Loadout.New(subTx2)
                {
                    Name = "Bar"
                };

                subTx2.CommitToParent();
            }

            subTx1.CommitToParent();
        }

        var result = await tx.Commit();

        var loadout1 = Loadout.Load(result.Db, result[loadout1Id]);
        await Assert.That(loadout1.IsValid()).IsTrue();
        await Assert.That(loadout1.Name).IsEqualTo("Foo");

        var loadout2 = Loadout.Load(result.Db, result[loadout2Id]);
        await Assert.That(loadout2.IsValid()).IsTrue();
        await Assert.That(loadout2.Name).IsEqualTo("Bar");
    }
    
}
