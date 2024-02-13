using NexusMods.EventSourcing.Abstractions;
using File = NexusMods.EventSourcing.TestModel.Model.File;

namespace NexusMods.EventSourcing.Tests;

public class DbTests : AEventSourcingTest
{

    public DbTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes) : base(valueSerializers, attributes)
    {

    }

    [Fact]
    public void ReadDatomsForEntity()
    {
        var tx = Connection.BeginTransaction();


        for (ulong i = 0; i < 10; i++)
        {
            var fileId = tx.TempId();
            File.Path.Assert(fileId, $"C:\\test_{i}.txt", tx);
            File.Hash.Assert(fileId, i + 0xDEADBEEF, tx);
        }

        var oldTx = Connection.TxId;
        var result = tx.Commit();

        result.NewTx.Value.Should().Be(oldTx.Value + 1, "transaction id should be incremented by 1");
    }

}
