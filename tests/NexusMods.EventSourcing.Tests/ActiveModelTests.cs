using NexusMods.EventSourcing.Abstractions;
using File = NexusMods.EventSourcing.TestModel.Model.File;

namespace NexusMods.EventSourcing.Tests;

public class ActiveModelTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    : AEventSourcingTest(valueSerializers, attributes)
{

    [Fact]
    public void CanGetActiveModel()
    {
        var tx = Connection.BeginTransaction();
        var file = new File(tx)
        {
            Hash = 0x12345678,
            Path = "C:\\TestFile.txt",
            Index = 24,
        };
        var results = tx.Commit();

        var activeModel = Connection.GetActive<File>(results[file.Id]);

    }
}
