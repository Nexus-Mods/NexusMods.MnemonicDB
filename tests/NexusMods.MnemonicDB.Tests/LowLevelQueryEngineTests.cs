namespace NexusMods.MnemonicDB.Tests;

public class LowLevelQueryEngineTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{

    [Fact]
    public void CanGetSingleColumnResult()
    {
        using var result = Query.Query<int>("SELECT 1 AS value");
        result.ToList().Should().BeEquivalentTo([1]);
    }
    
    [Fact]
    public void CanGetMultipleColumnsResult()
    {
        using var result = Query.Query<(int, int)>("SELECT 1 AS value1, 42 AS value2");
        result.ToList().Should().BeEquivalentTo([(1, 42)]);
    }

    [Fact]
    public void CanReturnStringResults()
    {
        using var result = Query.Query<(string, string)>("SELECT 'foo' AS value, 'this is a test of a longer string that will not be inlined' AS long_value");
        result.ToList().Should().BeEquivalentTo([("foo", "this is a test of a longer string that will not be inlined")]);
    }
    
}
