using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.TestModel.Attributes;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class QueryEngineTests : AMnemonicDBTest
{
    public QueryEngineTests(IServiceProvider provider) : base(provider)
    {
    }

    [Fact]
    public async Task CanGetDatomsViaDatomsFunction()
    {
        await InsertExampleData();
        var data = QueryEngine.Query<List<(EntityId, string, string, TxId)>>("SELECT E, A::VARCHAR, V::VARCHAR, T from mdb_Datoms() ORDER BY T, E, A  DESC");

        await VerifyTable(data);
    }

    [Fact]
    public async Task CanGetDatomsForSpecificAttribute()
    {
        await InsertExampleData();
        var data = QueryEngine.Query<List<(EntityId, string, TxId)>>("SELECT E, V, T from mdb_Datoms(A := 'Mod/Name') ORDER BY T, E  DESC")
            .Select(x => (x.Item1, "Mod/Name", x.Item2, x.Item3));

        await VerifyTable(data);
    }

    [Fact]
    public async Task CanPushdownProjection()
    {
        await InsertExampleData();
        var data = QueryEngine.Query<List<(EntityId, string)>>("SELECT DISTINCT E, V from mdb_Datoms(A := 'Mod/Name')");

        data.Count.Should().Be(3);
    }

    [Fact]
    public async Task CanSelectFromModels()
    {
        await InsertExampleData();
        var data = QueryEngine.Query<List<(EntityId, string)>>("SELECT Id, Name FROM mdb_Mod()"); 

        data.Count.Should().Be(3);
    }

    [Fact]
    public async Task CanSelectTuples()
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
        
        var fileA = new File.New(tx)
        {
            Path = "test.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = mod.Id,
            LocationPath = (LocationId.Game, "1"),
            TupleTest = (EntityId.From(42), LocationId.Game, "bleh")
        };
        
        var fileB = new File.New(tx)
        {
            Path = "test2.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = mod.Id,
            LocationPath = (LocationId.Preferences, "2")
        };
        
        var fileC = new File.New(tx)
        {
            Path = "test3.txt",
            Hash = Hash.From(0xDEADBEEF),
            Size = Size.From(1),
            ModId = mod.Id
        };
        
        var result = await tx.Commit();
        
        var results = QueryEngine.Query<List<(EntityId, string, string, string)>>("SELECT Id, Path, LocationPath::VARCHAR, TupleTest::VARCHAR FROM mdb_File()");
        
        results.Count.Should().Be(3);
    }
    
    [Fact]
    public async Task CanSelectFromModelsWithJoin()
    {
        await InsertExampleData();
        //var data = QueryEngine.Query<List<(EntityId, string, string)>>("SELECT Id, Name, LoadoutId::VARCHAR FROM mdb_Mod() JOIN mdb_Loadout() ON Id = LoadoutId");
    }
}
