namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public class ArrayTest
{
    [Test]
    public async Task TestModel() => await Helper.TestSourceGenerator<ModelGenerator>();
}
