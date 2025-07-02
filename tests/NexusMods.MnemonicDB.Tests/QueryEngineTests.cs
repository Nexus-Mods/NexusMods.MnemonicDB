using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Tests;

public class QueryEngineTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    [Fact]
    public async Task CanQuery()
    {
        await InsertExampleData();
        var result = Query.Query<ulong>("SELECT Id from mdb_Mod()");

        result.Should().HaveCount(3);
        
        var result2 = Query.Query<(string, string?)>("SELECT Name, Description from mdb_Mod()");
        result2.Should().BeEquivalentTo<(string, string?)>([
            ("Mod1 - Updated", null),
            ("Mod2 - Updated", null),
            ("Mod3 - Updated", null)
        ]);
    }
    
}
