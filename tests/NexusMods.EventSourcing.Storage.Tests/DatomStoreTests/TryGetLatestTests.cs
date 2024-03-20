using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

namespace NexusMods.EventSourcing.Storage.Tests.DatomStoreTests;

public class TryGetLatestTests(IServiceProvider provider) : AStorageTest(provider)
{

    [Theory]
    [InlineData("")]
    [InlineData("Mod 1")]
    [InlineData("Some really long name that is definitely longer than most people would use for a mod name")]
    public async Task CanGetLatestValueAsOfTx(string value)
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, value)]);

        var realId = tx.Remaps[tmpId];

        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, tx.TxId, out var modName).Should().BeTrue();

        modName.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Mod 1")]
    [InlineData("Some really long name that is definitely longer than most people would use for a mod name")]
    public async Task CanGetLatestValueAsOfMaxTx(string value)
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, value)]);

        var realId = tx.Remaps[tmpId];

        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, TxId.MaxValue, out var modName).Should().BeTrue();

        modName.Should().Be(value);
    }

    [Theory]
    [InlineData("", "Mod 1")]
    [InlineData("Mod 1", "Some really long name that is definitely longer than most people would use for a mod name")]
    [InlineData("Some really long name that is definitely longer than most people would use for a mod name", "")]
    public async Task CanGetOldValuesViaTx(string first, string second)
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, first)]);

        var realId = tx.Remaps[tmpId];

        var oldTx = tx.TxId;

        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, TxId.MaxValue, out var modName).Should().BeTrue();
        modName.Should().Be(first);

        var tx2 = await DatomStore.Transact([ModAttributes.Name.Assert(realId, second)]);

        var newTx = tx2.TxId;


        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, newTx, out modName).Should().BeTrue();
        modName.Should().Be(second);

        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, TxId.MaxValue, out modName).Should().BeTrue();
        modName.Should().Be(second);

        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, oldTx, out modName).Should().BeTrue();
        modName.Should().Be(first);
    }


    [Theory]
    [InlineData("", "Mod 1")]
    [InlineData("Mod 1", "Some really long name that is definitely longer than most people would use for a mod name")]
    [InlineData("Some really long name that is definitely longer than most people would use for a mod name", "")]
    public async Task CanGetOldValuesViaFuzzyTx(string first, string second)
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, first)]);

        var realId = tx.Remaps[tmpId];

        var oldTx = tx.TxId;

        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, TxId.MaxValue, out var modName).Should().BeTrue();
        modName.Should().Be(first);

        TxId midTx = default!;
        {
            var tmpId2 = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 2));
            for (var i = 0; i < 10; i += 1)
            {
                var midResult = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId2, second)]);
                if (i == 5)
                    midTx = midResult.TxId;
            }
        }

        var tx2 = await DatomStore.Transact([ModAttributes.Name.Assert(realId, second)]);

        var newTx = tx2.TxId;


        // We can get newest value by it exact Tx
        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, newTx, out modName).Should().BeTrue();
        modName.Should().Be(second);

        // And by the max Tx
        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, TxId.MaxValue, out modName).Should().BeTrue();
        modName.Should().Be(second);

        // And the old value by the old Tx
        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, oldTx, out modName).Should().BeTrue();
        modName.Should().Be(first);

        // And the old value via an unrelated Tx (between the first two inserts to this entity)
        DatomStore.TryGetLatest<ModAttributes.Name, string>(realId, midTx, out modName).Should().BeTrue();
        modName.Should().Be(first);
    }
}
