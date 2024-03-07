using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;

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

    [Fact]
    public async Task CanFlushToDisk()
    {
        // Set the size super small so we get a flush on every transaction
        DatomStoreSettings.MaxInMemoryDatoms = 128;


        var nodes = TestDatoms(1024)
            .GroupBy(d => d.Item2, d => d.Item1)
            .OrderBy(d => d.Key)
            .ToArray();

        foreach (var node in nodes)
        {
            await DatomStore.Transact(node);
        }
    }
}
