using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

namespace NexusMods.EventSourcing.Storage.Tests.DatomStoreTests;

public class AttributesAndValuesForEntityTests(IServiceProvider provider) : AStorageTest(provider)
{

    [Fact]
    public async Task CanGetAttributesAndValuesOnAnEntity()
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, "Mod 1")]);

        var realId = tx.Remaps[tmpId];

        var attributes = DatomStore.GetAttributesForEntity(realId, TxId.MaxValue).ToList();

        attributes.OfType<ModAttributes.Name.ReadDatom>()
            .Where(d => d.E == realId)
            .Should().ContainSingle().Which.V.Should().Be("Mod 1");
    }

}
