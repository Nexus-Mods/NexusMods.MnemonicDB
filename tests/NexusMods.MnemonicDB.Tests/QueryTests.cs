using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using ObservableCollections;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

[WithServiceProvider]
public class QueryTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    
    [Test]
    public async Task CanRunActiveQueries()
    {
        var table = TableResults();
        await InsertExampleData();

        var results = new ObservableList<(RelativePath, Hash, long)>();
        var historyResults = new ObservableList<(TxId, long)>();
        
        using var _ = Connection.Query<(RelativePath, Hash, long)>("SELECT Path, Hash, COUNT(*) FROM mdb_File() GROUP BY Path, Hash ORDER BY Path, Hash")
            .ObserveInto(results);
        using var _2 = Connection.Query<(TxId, long)>("SELECT T, COUNT(E) FROM mdb_Datoms(A:= 'File/Hash', History:=true) WHERE IsRetract = false GROUP BY T ORDER BY T")
            .ObserveInto(historyResults);

        table.Add(results, "Initial Results");
        table.Add(historyResults, "Initial History");
            
        await Assert.That(results).IsNotEmpty();
        

        // Update one hash to check that the queries update correctly
        using var tx = Connection.BeginTransaction();
        var ent = File.FindByPath(Connection.Db, "File1").First();
        tx.Add(ent.Id, File.Hash, Hash.FromLong(0x42));
        await tx.Commit();

        await Connection.FlushQueries();

        table.Add(results, "After Updates Query");
        table.Add(historyResults, "After Updates History");
        
        await Verify(table.ToString());
    }


    [Test]
    public async Task CanGetLatestTxForEntity()
    {
        var table = TableResults();
        await InsertExampleData();

        var results = new ObservableList<(EntityId, TxId)>();
        using var _ = Connection.Query<(EntityId, TxId)>("""
                                                         SELECT ents.Loadout, max(d.T) FROM 
                                                         (SELECT Id, Id as Loadout FROM mdb_Loadout() 
                                                          UNION SELECT Id, Loadout FROM mdb_Mod()
                                                          UNION SELECT file.Id, mod.Loadout FROM mdb_File() file 
                                                                LEFT JOIN mdb_Mod() mod ON mod.Id = file.Mod) ents
                                                         LEFT JOIN mdb_Datoms() d ON d.E = ents.Id
                                                         GROUP BY ents.Loadout
                                                         """).ObserveInto(results);
        
        table.Add(results, "Initial Results");
        
        // Update one hash to check that the queries update correctly
        using var tx = Connection.BeginTransaction();
        var ent = File.FindByPath(Connection.Db, "File1").First();
        tx.Add(ent.Id, File.Hash, Hash.FromLong(0x42));
        await tx.Commit();
        
        await Connection.FlushQueries();

        table.Add(results, "After Update");

        await Verify(table.ToString());

    }
    
    [Test]
    public async Task CanGetLatestTxForEntityOnDeletedEntities()
    {
        var tableResults = TableResults();
        await InsertExampleData();
        
        var results = new List<(EntityId, TxId, long)>();
        using var _ = Connection.Query<(EntityId, TxId, long)>("""
                                                               SELECT ents.Loadout, max(d.T), count(d.E) FROM 
                                                               (SELECT Id, Id as Loadout FROM mdb_Loadout() 
                                                                UNION SELECT Id, Loadout FROM mdb_Mod()
                                                                UNION SELECT file.Id, mod.Loadout FROM mdb_File() file 
                                                                      LEFT JOIN mdb_Mod() mod ON mod.Id = file.Mod) ents
                                                               LEFT JOIN mdb_Datoms() d ON d.E = ents.Id
                                                               GROUP BY ents.Loadout
                                                               """).ObserveInto(results);
        tableResults.Add(results, "Initial Results");
        
        // Delete only one datom to check that we get an update even for retracted datoms, as long as the entity is still present
        using var tx = Connection.BeginTransaction();
        var ent = File.FindByPath(Connection.Db, "File1").First();
        tx.Retract(ent.Id, File.Hash, ent.Hash);
        await tx.Commit();

        await Connection.FlushQueries();

        tableResults.Add(results, "After Retract");
        
        using var tx2 = Connection.BeginTransaction();
        tx2.Delete(ent.Id, recursive: false);
        await tx2.Commit();
        
        await Connection.FlushQueries();
        
        tableResults.Add(results, "After Delete");

        await Verify(tableResults.ToString());
    }

    [Test]
    public async Task TestMissingAndHaveQueries()
    {
        await InsertExampleData();

        var mods = Connection.Query<(EntityId, EntityId)>("SELECT Id, Mod FROM mdb_File()");
        var testMod = mods.First().Item2;
        
        using var tx = Connection.BeginTransaction();
        tx.Add(testMod, Mod.Marked, Null.Instance);
        await tx.Commit();

        var markedMods = Connection.Query<(EntityId, string)>("SELECT Id, Name FROM mdb_Mod() WHERE Marked = true");
        await Assert.That(markedMods).HasCount(1);

        var unmarkedMods = Connection.Query<(EntityId, string)>("SELECT Id, Name FROM mdb_Mod() WHERE Marked = false");
        await Assert.That(unmarkedMods).HasCount(2);
        
        using var tx2 = Connection.BeginTransaction();
        tx2.Add(testMod, Mod.Description, "Test Mod Description");
        await tx2.Commit();

        var modsWithDescription = Connection.Query<(EntityId, string)>("SELECT Id, Name FROM mdb_Mod() WHERE Description IS NOT NULL");
        await Assert.That(modsWithDescription).HasCount(1);

        var modsWithoutDescription = Connection.Query<(EntityId, string)>("SELECT Id, Name FROM mdb_Mod() WHERE Description IS NULL");
        await Assert.That(modsWithoutDescription).HasCount(2);
    }

    
    [Test]
    public async Task CanSendParametersToQuery()
    {
        var result = Connection.Query<int>("SELECT $1", 42);

        await Assert.That(result.First()).IsEqualTo(42);
    }
    
    [Test]
    public async Task CanPassDBToQuery()
    {
        const string queryText = "SELECT Id, Name FROM mdb_Mod(Db=>$1)";
        await InsertExampleData();

        var firstDb = Connection.Db;
        var result = Connection.Query<(EntityId, string)>(queryText, firstDb).ToArray();
        
        var table = TableResults();
        
        table.Add(result, "Initial Results");
        
        var id1 = result.First().Item1;
        using var tx = Connection.BeginTransaction();
        tx.Add(id1, Mod.Name, "Renamed - Mod");
        await tx.Commit();
        
        table.Add( Connection.Query<(EntityId, string)>(queryText, firstDb), "After update, old DB");
        
        table.Add( Connection.Query<(EntityId, string)>(queryText, Connection.Db), "After update, new DB");
        
        await Verify(table.ToString());
    }
    
    [Test]
    public async Task CanQueryWithoutPassing()
    {
        const string queryText = "SELECT Id, Name FROM mdb_Mod()";
        await InsertExampleData();

        var firstDb = Connection.Db;
        var result = Connection.Query<(EntityId, string)>(queryText, firstDb).ToArray();
        
        var table = TableResults();
        
        table.Add(result, "Initial Results");
        
        var id1 = result.First().Item1;
        using var tx = Connection.BeginTransaction();
        tx.Add(id1, Mod.Name, "Renamed - Mod");
        await tx.Commit();
        
        table.Add( Connection.Query<(EntityId, string)>(queryText), "After update, new DB");
        
        await Verify(table.ToString());
    }
}
