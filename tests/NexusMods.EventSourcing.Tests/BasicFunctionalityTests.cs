using System.Collections.Specialized;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public class BasicFunctionalityTests
{
    private readonly IEntityContext _ctx;
    public BasicFunctionalityTests(EventSerializer serializer)
    {
        var store = new InMemoryEventStore<EventSerializer>(serializer);
        _ctx = new EntityContext(store);
    }



    [Fact]
    public void CanSetupBasicLoadout()
    {
        EntityId<Loadout> loadoutId;
        using (var tx = _ctx.Begin())
        {
            loadoutId = CreateLoadout.Create(tx, "Test");
            tx.Commit();
        }
        var loadout = _ctx.Get(loadoutId);
        loadout.Should().NotBeNull();
        loadout.Name.Should().Be("Test");
    }

    [Fact]
    public void ChangingPropertyChangesTheValue()
    {
        using var tx = _ctx.Begin();
        var loadoutId = CreateLoadout.Create(tx, "Test");
        tx.Commit();

        var loadout = _ctx.Get(loadoutId);
        loadout.Name.Should().Be("Test");

        using var tx2 = _ctx.Begin();
        _ctx.Add(new RenameLoadout(loadoutId, "New Name"));
        tx2.Commit();

        loadout.Name.Should().Be("New Name");
    }

    [Fact]
    public void CanLinkEntities()
    {
        using var tx = _ctx.Begin();
        var loadoutId = CreateLoadout.Create(tx, "Test");
        var modId = AddMod.Create(tx, "First Mod", loadoutId);
        tx.Commit();

        var loadout = _ctx.Get(loadoutId);
        loadout.Mods.Count().Should().Be(1);
        loadout.Mods.First().Name.Should().Be("First Mod");

        var mod = _ctx.Get(modId);
        mod.Loadout.Should().NotBeNull();
        mod.Loadout.Name.Should().Be("Test");
    }


    [Fact]
    public void CanDeleteEntities()
    {
        using var tx = _ctx.Begin();
        var loadoutId = CreateLoadout.Create(tx, "Test");
        var modId = AddMod.Create(tx, "First Mod", loadoutId);
        tx.Commit();

        var loadout = _ctx.Get(loadoutId);
        loadout.Mods.Count().Should().Be(1);
        loadout.Mods.First().Name.Should().Be("First Mod");

        var mod = _ctx.Get(modId);
        mod.Loadout.Should().NotBeNull();
        mod.Loadout.Name.Should().Be("Test");

        using var tx2 = _ctx.Begin();
        DeleteMod.Create(tx2, modId, loadoutId);
        tx2.Commit();

        loadout.Mods.Count().Should().Be(0);
    }

    [Fact]
    public void CanGetSingletonEntities()
    {
        var entity = _ctx.Get<LoadoutRegistry>();
        entity.Should().NotBeNull();
    }

    [Fact]
    public void UpdatingAValueCallesNotifyPropertyChanged()
    {
        var loadouts = _ctx.Get<LoadoutRegistry>();
        loadouts.Loadouts.Should().BeEmpty();

        using var tx = _ctx.Begin();
        var loadoutId = CreateLoadout.Create(tx, "Test");
        tx.Commit();

        var loadout = _ctx.Get(loadoutId);

        var called = false;
        loadout.PropertyChanged += (sender, args) =>
        {
            called = true;
            args.PropertyName.Should().Be(nameof(Loadout.Name));
        };

        using var tx2 = _ctx.Begin();
        RenameLoadout.Create(tx2, loadoutId, "New Name");
        tx2.Commit();

        called.Should().BeTrue();
        loadout.Name.Should().Be("New Name");
    }

    [Fact]
    public void EntityCollectionsAreObservable()
    {
        using var tx = _ctx.Begin();
        var loadoutId = CreateLoadout.Create(tx, "Test");
        var modId = AddMod.Create(tx, "First Mod", loadoutId);
        tx.Commit();

        var loadoutRegistry = _ctx.Get<LoadoutRegistry>();
        loadoutRegistry.Loadouts.Should().NotBeEmpty();

        var called = false;
        ((INotifyCollectionChanged)loadoutRegistry.Loadouts).CollectionChanged += (sender, args) =>
        {
            called = true;
            args.Action.Should().Be(NotifyCollectionChangedAction.Add);
            args.NewItems.Should().NotBeNull();
        };

        using var tx2 = _ctx.Begin();
        var loadout2Id = CreateLoadout.Create(tx2, "Test 2");
        tx2.Commit();

        called.Should().BeTrue();
        loadoutRegistry.Loadouts.Should().NotBeEmpty();
    }

    [Fact]
    public void CanCreateCyclicDependencies()
    {
        var loadouts = _ctx.Get<LoadoutRegistry>();

        using (var tx = _ctx.Begin())
        {

            var loadoutId = CreateLoadout.Create(tx, "Test");

            var mod1 =AddMod.Create(tx, "First Mod", loadoutId);
            var mod2 = AddMod.Create(tx, "Second Mod", loadoutId);

            var collection = AddCollection.Create(tx, "First Collection", loadoutId, mod1, mod2);

            tx.Commit();
        }

        var loadout = loadouts.Loadouts.First();
        loadout.Collections.Count.Should().Be(1);
        loadout.Collections.First().Mods.Count.Should().Be(2);
        loadout.Collections.First().Mods.First().Name.Should().Be("First Mod");
        loadout.Collections.First().Name.Should().Be("First Collection");
        loadout.Mods.Count.Should().Be(2);
        loadout.Mods.First().Name.Should().Be("First Mod");
        loadout.Mods.Last().Name.Should().Be("Second Mod");
        loadout.Mods.First().Collection.Name.Should().Be("First Collection");
    }
}
