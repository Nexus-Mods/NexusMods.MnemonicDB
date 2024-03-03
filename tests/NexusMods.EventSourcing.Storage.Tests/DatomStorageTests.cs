using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Tests;

public class DatomStorageTests(IServiceProvider provider) : AStorageTest(provider)
{

    [Fact]
    public void CanGetExistingAttributes()
    {
        var txId = DatomStore.AsOfTxId;
        txId.Should().Be(TxId.MinValueAfterBootstrap, "the database is bootstrapped with a single transaction");

        var datoms = DatomStore.Where<BuiltInAttributes.UniqueId>(txId)
            .Select(d => d.E)
            .ToArray();

        datoms.Should().Contain(EntityId.From(1));
        datoms.Should().Contain(EntityId.From(2));
    }
}
