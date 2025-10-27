namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public class QueryResultTest
{
    [Test]
    public async Task TestGenerator() => await Helper.TestIncrementalGenerator<SqlResultGenerator>();
}
