namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public class BasicTest
{
    [Test]
    public async Task TestModel() => await Helper.TestSourceGenerator<ModelGenerator>();
}
