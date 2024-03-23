using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

namespace NexusMods.EventSourcing.Storage.Tests.DatomStoreTests;

public class TryGetExactTests(IServiceProvider provider) : AStorageTest(provider)
{

    [Theory]
    [InlineData("")]
    [InlineData("Mod 1")]
    [InlineData("Some really long name that is definitely longer than most people would use for a mod name")]
    public async Task CanGetNewValue(string value)
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, value)]);

        var realId = tx.Remaps[tmpId];

        DatomStore.TryGetExact<ModAttributes.Name, string>(realId, tx.TxId, out var modName).Should().BeTrue();

        modName.Should().Be(value);
    }

    [Theory]
    [InlineData("", "Mod 1")]
    [InlineData("Mod 1", "Some really long name that is definitely longer than most people would use for a mod name")]
    [InlineData("Some really long name that is definitely longer than most people would use for a mod name", "")]
    public async Task UpdatingValueReturnsNewValue(string first, string second)
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));

        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, first)]);
        var realId = tx.Remaps[tmpId];

        DatomStore.TryGetExact<ModAttributes.Name, string>(realId, tx.TxId, out var modName).Should().BeTrue();
        modName.Should().Be(first);

        var tx2 = await DatomStore.Transact([ModAttributes.Name.Assert(realId, second)]);

        DatomStore.TryGetExact<ModAttributes.Name, string>(realId, tx2.TxId, out var modName2).Should().BeTrue();
        modName2.Should().Be(second);
    }

    [Theory]
    [InlineData("Mod 1", "Mod 2")]
    [InlineData("Mod 2", "Mod 3")]
    public async Task AssertsDoNotClobberOtherResults(string a, string b)
    {
        var tmpId1 = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tmpId2 = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 2));

        var tx = await DatomStore.Transact([
            ModAttributes.Name.Assert(tmpId1, a), ModAttributes.Name.Assert(tmpId2, b)
        ]);
        var realId1 = tx.Remaps[tmpId1];
        var realId2 = tx.Remaps[tmpId2];

        DatomStore.TryGetExact<ModAttributes.Name, string>(realId1, tx.TxId, out var modName1).Should().BeTrue();
        modName1.Should().Be(a);

        DatomStore.TryGetExact<ModAttributes.Name, string>(realId2, tx.TxId, out var modName2).Should().BeTrue();
        modName2.Should().Be(b);
    }

    [Fact]
    public void InvalidIdReturnsFalse()
    {
        DatomStore.TryGetExact<ModAttributes.Name, string>(EntityId.From(Ids.MakeId(Ids.Partition.Entity, ulong.MaxValue)),
            TxId.From(1), out var modName)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Mod 1")]
    [InlineData("Some really long name that is definitely longer than most people would use for a mod name")]
    public async Task InvalidTxReturnsFalse(string value)
    {
        var tmpId = EntityId.From(Ids.MakeId(Ids.Partition.Tmp, 1));
        var tx = await DatomStore.Transact([ModAttributes.Name.Assert(tmpId, value)]);
        var realId = tx.Remaps[tmpId];

        DatomStore.TryGetExact<ModAttributes.Name, string>(realId, TxId.From(1), out var modName)
            .Should().BeFalse();
    }
}
