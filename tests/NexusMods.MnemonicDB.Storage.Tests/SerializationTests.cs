using System.Text.Json;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class SerializationTests
{
    [Fact]
    public void TestJson()
    {
        var expected = FileId.From(1337);

        var json = JsonSerializer.Serialize(expected);
        json.Should().Be("1337");

        var actual = JsonSerializer.Deserialize<FileId>(json);
        actual.Should().Be(expected);
    }
}
