using DynamicData;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Patterns;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class QueryTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    [Fact]
    public async Task CanGetDatoms()
    {
        await InsertExampleData();
        var db = Connection.Db;

        var flow = from p in File.Path
            select p;

        var results = db.Topology.Outlet(flow);

        results.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task CanFilterAndSelectDatoms()
    {
        await InsertExampleData();
        var db = Connection.Db;
        
        var flow = 
            File.Path
                .Where(a => a.Value == "File1")
                .LeftInnerJoin(File.Hash)
                .Select(r => (r.Key, r.Value.Item1, r.Value.Item2));

        var results = db.Topology.Outlet(flow);
        
        results.Should().NotBeEmpty();
    }

    
    [Fact]
    public async Task CanRunActiveQueries()
    {
        await InsertExampleData();
        
        
        var query = 
            Pattern.Create()
                .Db(out var e, File.Path, out var path)
                .Db(e, File.Hash, out var hash)
                .Return(path, hash, path.Count());
        
        
        var results = Connection.Topology.Outlet(query);
        
        results.Should().NotBeEmpty();
        
        // Query how many hashes are modified in each transaction
        var historyQuery =
            Pattern.Create()
                .DbHistory(e, File.Hash, hash, out var txId)
                // Strip the partition from the txId, just to make it easier to read
                .Project(txId, t => t.ValuePortion, out var txNoPartition)
                .Return(txNoPartition, e.Count());
        
        var historyResults = Connection.Topology.Outlet(historyQuery);

        // Validate the results
        
        // Three mods with overlapping filenames, so we expect 3 results for each
        results.Should().BeEquivalentTo(new (RelativePath, Hash, int)[] {
            ("File1", Hash.FromLong(0xDEADBEEF), 3),
            ("File2", Hash.FromLong(0xDEADBEF0), 3),
            ("File3", Hash.FromLong(0xDEADBEF1), 3),
        });
        
        historyResults.Should().BeEquivalentTo(new (ulong, int)[] {
            // 9 hashes in the first transaction
            (3, 9),
        });

        // Update one hash to check that the queries update correctly
        using var tx = Connection.BeginTransaction();
        var ent = File.FindByPath(Connection.Db, "File1").First();
        tx.Add(ent.Id, File.Hash, Hash.FromLong(0x42));
        await tx.Commit();
        
        // we swapped one file over to a different hash, so we should see 4 results now with 2 count for one
        // and 1 for the other
        results.Should().BeEquivalentTo(new (RelativePath, Hash, int)[] {
            ("File1", Hash.FromLong(0x42), 1),
            ("File2", Hash.FromLong(0xDEADBEF0), 3),
            ("File3", Hash.FromLong(0xDEADBEF1), 3),
            ("File1", Hash.FromLong(0xDEADBEEF), 2),
        });


        
        historyResults.Should().NotBeEmpty();
        historyResults.Should().BeEquivalentTo(new (ulong, int)[] {
            // 9 hashes in the first transaction
            (3, 9),
            // 1 hash in the second transaction
            (5, 1)
        });

    }
}
