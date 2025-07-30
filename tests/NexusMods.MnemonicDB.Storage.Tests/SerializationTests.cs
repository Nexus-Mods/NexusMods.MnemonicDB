using System.Text.Json;
using NexusMods.MnemonicDB.TestModel;
using Assert = TUnit.Assertions.Assert;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class SerializationTests
{
    [Test]
    public async Task TestJson()
    {
        var expected = FileId.From(1337);

        var json = JsonSerializer.Serialize(expected);
        await Assert.That(json).IsEqualTo("1337");

        var actual = JsonSerializer.Deserialize<FileId>(json);
        await Assert.That(actual).IsEqualTo(expected);
    }
}
