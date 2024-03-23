using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

namespace NexusMods.EventSourcing.Storage.Tests.DatomStoreTests;

public class ByAttributeTests(IServiceProvider provider) : AStorageTest(provider)
{

    [Fact]
    public async Task GetEntitiesWithAttributes()
    {

        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, "Mod 1")]);

        var realId = tx.Remaps[tmpId];

        var entities = DatomStore.GetEntitiesWithAttribute<ModAttributes.Name>(TxId.MaxValue).ToList();

        entities.Should().Contain(realId);

    }

}
