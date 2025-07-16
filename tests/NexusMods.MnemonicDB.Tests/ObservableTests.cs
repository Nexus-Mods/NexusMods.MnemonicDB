using DynamicData;
using DynamicData.Alias;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.Tests;

[WithServiceProvider]
public class ObservableTests : AMnemonicDBTest
{
    public ObservableTests(IServiceProvider provider) : base(provider) { }

    [Test]
    public async Task TestObserveAll()
    {
        throw new NotImplementedException();
        /*
        using var disposable = Loadout
            .ObserveAll(Connection)
            .Transform(loadout => loadout.Name)
            .Bind(out var list)
            .Subscribe();

        await Assert.That(list).IsEmpty();

        await Add("Loadout 1");

        await Assert.That(list.Should().ContainSingle(static name => name.Equals("Loadout 1")));

        await Add("Loadout 2");

        list.Should().HaveCount(2).And.ContainInOrder(["Loadout 1", "Loadout 2"]);

        await Add("Loadout 3", "Loadout 4");

        list.Should().HaveCount(4).And.ContainInOrder(["Loadout 1", "Loadout 2", "Loadout 3", "Loadout 4"]);

        await Delete("Loadout 2");

        list.Should().HaveCount(3).And.ContainInOrder(["Loadout 1", "Loadout 3", "Loadout 4"]);

        await Delete("Loadout 1", "Loadout 4");

        list.Should().ContainSingle(static name => name.Equals("Loadout 3"));

        await Delete("Loadout 3");

        list.Should().BeEmpty();
        */
    }

    /*
    [Test]
    public async Task StressTest()
    {
        using var disposable = Loadout
            .ObserveAll(Connection)
            .Transform(loadout => loadout.Name)
            .Bind(out var list)
            .Subscribe();

        var stress = Enumerable.Range(start: 0, count: 100).Select(i => $"Loadout {i}").ToArray();
        await Add(stress);

        list.Should().ContainInOrder(stress);

        await Delete(stress);

        list.Should().BeEmpty();
    }

    private async ValueTask Add(params string[] names)
    {
        using var tx = Connection.BeginTransaction();

        foreach (var name in names)
        {
            _ = new Loadout.New(tx)
            {
                Name = name,
            };
        }


        await tx.Commit();
    }

    private async ValueTask Delete(params string[] names)
    {
        var db = Connection.Db;
        using var tx = Connection.BeginTransaction();

        foreach (var name in names)
        {
            var entities = Loadout.FindByName(db, name);
            entities.Should().ContainSingle();

            tx.Delete(entities.First(), recursive: false);
        }

        await tx.Commit();
    }
    */
}
