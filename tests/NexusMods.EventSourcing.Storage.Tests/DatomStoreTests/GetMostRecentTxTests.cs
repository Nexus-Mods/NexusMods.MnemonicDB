using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

namespace NexusMods.EventSourcing.Storage.Tests.DatomStoreTests;

public class GetMostRecentTxTests(IServiceProvider provider) : AStorageTest(provider)
{
    [Fact]
    public async Task CanGetLatestTx()
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, "Mod 1")]);

        var latestTx = DatomStore.GetMostRecentTxId();

        latestTx.Should().Be(tx.TxId);
    }
}
