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
        await _ctx.Transact(createEvent);
        var loadout = await _ctx.Retrieve(createEvent.Id);
        loadout.Name.Should().Be("Test");
    }
}
