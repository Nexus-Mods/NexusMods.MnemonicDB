using NexusMods.MnemonicDB.Abstractions;

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
}
