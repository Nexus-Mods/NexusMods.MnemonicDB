using DynamicData;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Patterns;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using VerifyTUnit;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

[WithServiceProvider]
public class QueryTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    [Test]
    public async Task CanGetDatoms()
    {
        await InsertExampleData();
        var db = Connection.Db;

        var flow = from p in File.Path
            select p;

        using var results = await db.Topology.QueryAsync(flow);

        await Assert.That(results).IsNotEmpty();
    }
    
    [Test]
    public async Task CanFilterAndSelectDatoms()
    {
        await InsertExampleData();
        var db = Connection.Db;
        
        var flow = 
            File.Path
                .Where(a => a.Value == "File1")
                .LeftInnerJoin(File.Hash)
                .Select(r => (r.Key, r.Value.Item1, r.Value.Item2));

        using var results = await db.Topology.QueryAsync(flow);
        
        await Assert.That(results).IsNotEmpty();
    }

    
    [Test]
    public async Task CanRunActiveQueries()
    {
        await InsertExampleData();
        
        
        var query = 
            Pattern.Create()
                .Db(out var e, File.Path, out var path)
                .Db(e, File.Hash, out var hash)
                .Return(path, hash, path.Count());
        
        
        using var results = await Connection.Topology.QueryAsync(query);
        
        await Assert.That(results).IsNotEmpty();
        
        // Query how many hashes are modified in each transaction
        var historyQuery =
            Pattern.Create()
                .DbHistory(e, File.Hash, hash, out var txId)
                // Strip the partition from the txId, just to make it easier to read
                .Project(txId, t => t.ValuePortion, out var txNoPartition)
                .Return(txNoPartition, e.Count());
        
        using var historyResults = await Connection.Topology.QueryAsync(historyQuery);

        // Validate the results
        
        // Three mods with overlapping filenames, so we expect 3 results for each
        await Assert.That(results.ToArray()).IsEquatableOrEqualTo([
            ("File1", Hash.FromLong(0xDEADBEEF), 3),
            ("File2", Hash.FromLong(0xDEADBEF0), 3),
            ("File3", Hash.FromLong(0xDEADBEF1), 3)
        ]);
        
        await Assert.That(historyResults.ToArray()).IsEquatableOrEqualTo([
            // 9 hashes in the first transaction
            (3, 9)
        ]);

        // Update one hash to check that the queries update correctly
        using var tx = Connection.BeginTransaction();
        var ent = File.FindByPath(Connection.Db, "File1").First();
        tx.Add(ent.Id, File.Hash, Hash.FromLong(0x42));
        await tx.Commit();
        
        // we swapped one file over to a different hash, so we should see 4 results now with 2 count for one
        // and 1 for the other
        await Assert.That(results.ToArray()).IsEquatableOrEqualTo([
            ("File1", Hash.FromLong(0x42), 1),
            ("File2", Hash.FromLong(0xDEADBEF0), 3),
            ("File3", Hash.FromLong(0xDEADBEF1), 3),
            ("File1", Hash.FromLong(0xDEADBEEF), 2)
        ]);


        
        await Assert.That(historyResults).IsNotEmpty();
        await Assert.That(historyResults.ToArray()).IsEquatableOrEqualTo([
            // 9 hashes in the first transaction
            (3, 9),
            // 1 hash in the second transaction
            (5, 1)
        ]);

    }

    [Test]
    public async Task CanGetLatestTxForEntity()
    {
        await InsertExampleData();

        var query = Pattern.Create()
            .Db(out var e, File.ModId, out var mod)
            .Db(mod, Mod.Loadout, out var loadout)
            .DbLatestTx(e, out var txId)
            .Return(loadout, txId.Max());

        using var queryResults = await Connection.Topology.QueryAsync(query);
        
        var oldData = queryResults.ToList();
        
        // Update one hash to check that the queries update correctly
        using var tx = Connection.BeginTransaction();
        var ent = File.FindByPath(Connection.Db, "File1").First();
        tx.Add(ent.Id, File.Hash, Hash.FromLong(0x42));
        await tx.Commit();
        
        var newData = queryResults.ToList();

        var diagram = Connection.Topology.Diagram();
        await Verify(new
        {
            OldData = oldData.Select(row => row.ToString()).ToArray(),
            NewData = newData.Select(row => row.ToString()).ToArray(),
        });

    }
    
    [Test]
    public async Task CanGetLatestTxForEntityOnDeletedEntities()
    {
        await InsertExampleData();

        var query = Pattern.Create()
            .Db(out var e, File.ModId, out var mod)
            .Db(mod, Mod.Loadout, out var loadout)
            .DbLatestTx(e, out var txId)
            .Return(loadout, txId.Max(), e.Count());
        
        using var queryResults = await Connection.Topology.QueryAsync(query);
        
        var oldData = queryResults.ToList();
        
        // Delete only one datom to check that we get an update even for retracted datoms, as long as the entity is still present
        using var tx = Connection.BeginTransaction();
        var ent = File.FindByPath(Connection.Db, "File1").First();
        tx.Retract(ent.Id, File.Path, (RelativePath)"File1");
        await tx.Commit();
        
        var withoutName = queryResults.ToList();
        
        using var tx2 = Connection.BeginTransaction();
        tx2.Delete(ent.Id, recursive: false);
        await tx2.Commit();
        
        var deletedEntity = queryResults.ToList();

        await Verify(new
        {
            Entry1 = oldData.Select(row => row.ToString()).ToArray(),
            Entry2 = withoutName.Select(row => row.ToString()).ToArray(),
            Entry3 = deletedEntity.Select(row => row.ToString()).ToArray(),
        });

    }

    [Test]
    public async Task TestMissingAndHaveQueries()
    {
        await InsertExampleData();

        var mods = Pattern.Create()
            .Db(out var e, File.ModId, out var mod)
            .Return(e, mod);
        
        var results = await Connection.Topology.QueryAsync(mods);

        var testMod = results.First().Item2;
        
        using var tx = Connection.BeginTransaction();
        tx.Add(testMod, Mod.Marked, Null.Instance);
        await tx.Commit();
        
        var markedMods = Pattern.Create()
            .Db(mod, Mod.Name, out var name)
            .HasAttribute(mod, Mod.Marked)
            .Return(mod, name);
        
        using var markedResults = await Connection.Topology.QueryAsync(markedMods);
        await Assert.That(markedResults).HasCount(1);
        
        var unmarkedMods = Pattern.Create()
            .Db(mod, Mod.Name, out var name2)
            .MissingAttribute(mod, Mod.Marked)
            .Return(mod, name2);
        
        using var unmarkedResults = await Connection.Topology.QueryAsync(unmarkedMods);
        await Assert.That(unmarkedResults).HasCount(2);
        
        using var tx2 = Connection.BeginTransaction();
        tx2.Add(testMod, Mod.Description, "Test Mod Description");
        await tx2.Commit();
        
        var modsWithDescription = Pattern.Create()
            .Db(mod, Mod.Name, name)
            .HasAttribute(mod, Mod.Description)
            .Return(mod, name);
        
        using var descriptionResults = await Connection.Topology.QueryAsync(modsWithDescription);
        await Assert.That(descriptionResults).HasCount(1);
        
        var modsWithoutDescription = Pattern.Create()
            .Db(mod, Mod.Name, name)
            .MissingAttribute(mod, Mod.Description)
            .Return(mod, name);
        
        using var noDescriptionResults = await Connection.Topology.QueryAsync(modsWithoutDescription);
        await Assert.That(noDescriptionResults).HasCount(2);
    }
}
