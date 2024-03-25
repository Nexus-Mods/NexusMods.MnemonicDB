using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

namespace NexusMods.EventSourcing.Storage.Tests.DatomStoreTests;

public class EntitiesWithAttributeTests(IServiceProvider provider) : AStorageTest(provider)
{

    [Fact]
    public async Task CanGetEntitiesWithAttribute()
    {
        var tmpId = NextTempId();
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, "Mod 1")]);

        var realId = tx.Remaps[tmpId];

        var entities = DatomStore.GetEntitiesWithAttribute<ModAttributes.Name>(TxId.MaxValue).ToList();

        entities.Should().Contain(realId);
    }

    [Fact]
    public async Task CanGetEntitiesWithAttributeAsOf()
    {
        var tmpId1 = NextTempId();
        var tx1 = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId1, "Mod 1")]);

        var tmpId2 = NextTempId();
        var tx2 = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId2, "Mod 2")]);


        DatomStore.GetEntitiesWithAttribute<ModAttributes.Name>(tx1.TxId)
            .Should().Contain(tx1.Remaps[tmpId1])
            .And.NotContain(tx2.Remaps[tmpId2]);

        DatomStore.GetEntitiesWithAttribute<ModAttributes.Name>(tx2.TxId)
            .Should()
            .Contain(tx1.Remaps[tmpId1])
            .And.Contain(tx2.Remaps[tmpId2]);

    }


}
