using NexusMods.EventSourcing.TestModel.Events;
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
    public async void CanSetupBasicLoadout()
    {
        var createEvent = CreateLoadout.Create("Test");
        await _ctx.Add(createEvent);
        var loadout = _ctx.Get(createEvent.Id);
        loadout.Should().NotBeNull();
        loadout.Name.Should().Be("Test");
    }

    [Fact]
    public async void ChangingPropertyChangesTheValue()
    {
        var createEvent = CreateLoadout.Create("Test");
        await _ctx.Add(createEvent);
        var loadout = _ctx.Get(createEvent.Id);
        loadout.Name.Should().Be("Test");

        await _ctx.Add(RenameLoadout.Create(createEvent.Id, "New Name"));
        loadout.Name.Should().Be("New Name");
    }
}
