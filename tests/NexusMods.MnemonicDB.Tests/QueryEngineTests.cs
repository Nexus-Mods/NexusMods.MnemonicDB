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
        var data = QueryEngine.Query<List<(EntityId, string, TxId)>>("SELECT E, A::VARCHAR, T from mdb_Datoms()");
        
        data.Should().NotBeEmpty();
    }
}
