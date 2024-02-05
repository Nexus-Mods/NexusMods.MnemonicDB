using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;


public class DataModelTests(IConnection connection)
{
    [Fact]
    public void CanInsertMods()
    {
        {
            using var tx = connection.BeginTransaction();
            var mod = new Mod(tx)
            {
                Name = "Test Mod",
                Description = "This is a test mod",
            };

            tx.Commit();
        }
    }
}
