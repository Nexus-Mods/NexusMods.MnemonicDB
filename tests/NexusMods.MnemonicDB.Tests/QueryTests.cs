using NexusMods.Cascade;
using NexusMods.Cascade.Patterns;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
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
        var table = TableResults();
        await InsertExampleData();

        var results = Connection.Query<List<(RelativePath, Hash, int)>>("SELECT Path, Hash, COUNT(*) FROM mdb_File() GROUP BY Path, Hash");
        table.Add(results, "Initial Results");
            
        await Assert.That(results).IsNotEmpty();

        var historyQuery =
            Connection.Query<List<(TxId, int)>>(
                "SELECT T, COUNT(E) FROM mdb_Datoms(A:= 'File/Hash', History:=true) GROUP BY T");
        
        table.Add(historyQuery, "Initial History");

        // Update one hash to check that the queries update correctly
        using var tx = Connection.BeginTransaction();
        var ent = File.FindByPath(Connection.Db, "File1").First();
        tx.Add(ent.Id, File.Hash, Hash.FromLong(0x42));
        await tx.Commit();
        
        
        
        await Verify(table.ToString());
/*

        await Assert.That(AreEqual(results.ToList(), [
            ("File1", Hash.FromLong(0xDEADBEEF), 3),
            ("File2", Hash.FromLong(0xDEADBEF0), 3),
            ("File3", Hash.FromLong(0xDEADBEF1), 3)
        ])).IsTrue();

        await Assert.That(AreEqual(historyQuery, new (ulong, int)[]{
            // 9 hashes in the first transaction
            (3, 9)
        })).IsTrue();;



        await Verify(results);
        // we swapped one file over to a different hash, so we should see 4 results now with 2 count for one
        // and 1 for the other
        await Assert.That(AreEqual(results.ToList(), new (RelativePath, Hash, int)[]{
            ("File1", Hash.FromLong(0x42), 1),
            ("File2", Hash.FromLong(0xDEADBEF0), 3),
            ("File3", Hash.FromLong(0xDEADBEF1), 3),
            ("File1", Hash.FromLong(0xDEADBEEF), 2)
        })).IsTrue();


        await Assert.That(historyResults).IsNotEmpty();
        await Assert.That(AreEqual(historyResults.ToList(), new (ulong, int)[]{
            // 9 hashes in the first transaction
            (3, 9),
            // 1 hash in the second transaction
            (5, 1)
        })).IsTrue();

        bool AreEqual<T>(List<T> a, T[] b)
        {
            if (a.Count != b.Length) return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (!a.Contains(b[i]))
                    return false;
            }
            return true;
        }
        */
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

        var mods = Connection.Query<List<(EntityId, EntityId)>>("SELECT Id, Mod FROM mdb_File()");
        var testMod = mods.First().Item2;
        
        using var tx = Connection.BeginTransaction();
        tx.Add(testMod, Mod.Marked, Null.Instance);
        await tx.Commit();

        var markedMods = Connection.Query<List<(EntityId, string)>>("SELECT Id, Name FROM mdb_Mod() WHERE Marked = true");
        
        await Assert.That(markedMods).HasCount(1);

        var unmarkedMods = Connection.Query<List<(EntityId, string)>>("SELECT Id, Name FROM mdb_Mod() WHERE Marked = false");
        await Assert.That(unmarkedMods).HasCount(2);
        
        using var tx2 = Connection.BeginTransaction();
        tx2.Add(testMod, Mod.Description, "Test Mod Description");
        await tx2.Commit();

        var modsWithDescription = Connection.Query<List<(EntityId, string)>>("SELECT Id, Name FROM mdb_Mod() WHERE Description IS NOT NULL");
        await Assert.That(modsWithDescription).HasCount(1);

        var modsWithoutDescription = Connection.Query<List<(EntityId, string)>>("SELECT Id, Name FROM mdb_Mod() WHERE Description IS NULL");
        await Assert.That(modsWithoutDescription).HasCount(2);
    }
}
