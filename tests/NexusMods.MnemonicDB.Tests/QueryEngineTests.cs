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
        var data = QueryEngine.Query<List<(EntityId, string, string, TxId)>>("SELECT E, A::VARCHAR, V::VARCHAR, T from mdb_Datoms() WHERE V is not null");

        data.Count.Should().Be(42);
    }
}
