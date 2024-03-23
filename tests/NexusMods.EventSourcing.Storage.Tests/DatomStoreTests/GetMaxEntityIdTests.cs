using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

namespace NexusMods.EventSourcing.Storage.Tests.DatomStoreTests;

public class GetMaxEntityIdTests(IServiceProvider provider) : AStorageTest(provider)
{
    [Fact]
    public async Task CanGetMaxEntityId()
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, "Mod 1")]);

        var realId = tx.Remaps[tmpId];

        var maxId = DatomStore.GetMaxEntityId();

        maxId.Should().Be(realId);
    }

}
