using DynamicData;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
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
        
        Assert.NotEmpty(results.Values);
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
        
        Assert.NotEmpty(results.Values);
    }

    /*
    [Fact]
    public async Task CanRunActiveQueries()
    {
        await InsertExampleData();
        
        IQuery<(EntityId Id, RelativePath Path, Hash Hash)> query = 
            Query.Where(File.Path, "File1")
                .Select(File.Path, File.Hash);
        
        var results = Connection.Flow.Query(query);
        //var activeQuery = Connection.Flow.Update(ops => ops.ObserveAllResults(query));
        
        Assert.NotEmpty(results);

        using var tx = Connection.BeginTransaction();
        foreach (var (id, path, hash) in results)
        {
            tx.Add(id, File.Hash, Hash.From(hash.Value + 42));
        }
        await tx.Commit();
        
        var updatedResults = Connection.Flow.Query(query);
        
        Assert.NotEmpty(updatedResults);
    }
    */
}
