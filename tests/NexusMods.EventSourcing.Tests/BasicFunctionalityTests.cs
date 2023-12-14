using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.EventSourcing.Tests.Contexts;

namespace NexusMods.EventSourcing.Tests;

public class BasicFunctionalityTests
{
    private readonly TestContext _ctx;
    public BasicFunctionalityTests(TestContext ctx)
    {
        _ctx = ctx;
    }



    [Fact]
    public void CanSetupBasicLoadout()
    {
        var createEvent = CreateLoadout.Create("Test");
        _ctx.Add(createEvent);
        var loadout = _ctx.Get(createEvent.Id);
        loadout.Should().NotBeNull();
        loadout.Name.Should().Be("Test");
    }

    [Fact]
    public void ChangingPropertyChangesTheValue()
    {
        var createEvent = CreateLoadout.Create("Test");
        _ctx.Add(createEvent);
        var loadout = _ctx.Get(createEvent.Id);
        loadout.Name.Should().Be("Test");

        _ctx.Add(new RenameLoadout(createEvent.Id, "New Name"));
        loadout.Name.Should().Be("New Name");
    }

    [Fact]
    public void CanLinkEntities()
    {
        var loadoutEvent = CreateLoadout.Create("Test");
        _ctx.Add(loadoutEvent);
        var loadout = _ctx.Get(loadoutEvent.Id);
        loadout.Name.Should().Be("Test");

        var modEvent = AddMod.Create("First Mod", loadoutEvent.Id);
        _ctx.Add(modEvent);

        loadout.Mods.First().Name.Should().Be("First Mod");
        loadout.Mods.First().Loadout.Should().BeSameAs(loadout);
    }


    [Fact]
    public async void CanDeleteEntities()
    {
        var loadoutEvent = CreateLoadout.Create("Test");
        _ctx.Add(loadoutEvent);
        var loadout = _ctx.Get(loadoutEvent.Id);
        loadout.Name.Should().Be("Test");

        var modEvent1 = AddMod.Create("First Mod", loadoutEvent.Id);
        _ctx.Add(modEvent1);

        var modEvent2 = AddMod.Create("Second Mod", loadoutEvent.Id);
        _ctx.Add(modEvent2);

        loadout.Mods.Count().Should().Be(2);

        _ctx.Add(new DeleteMod(modEvent1.ModId, loadoutEvent.Id));

        loadout.Mods.Count().Should().Be(1);

        loadout.Mods.First().Name.Should().Be("Second Mod");
    }

    [Fact]
    public void CanGetSingletonEntities()
    {
        var entity = _ctx.Get<LoadoutRegistry>();
        entity.Should().NotBeNull();
    }
}
