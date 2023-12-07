using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Tests.Contexts;
using NexusMods.EventSourcing.Tests.DataObjects;
using NexusMods.EventSourcing.Tests.Events;

namespace NexusMods.EventSourcing.Tests;

public class BasicFunctionalityTests
{
    private readonly TestContext _ctx;
    public BasicFunctionalityTests(TestContext ctx)
    {
        _ctx = ctx;
    }

    [Fact]
    public async void CanApplyEvents()
    {
        var newId = EntityId<CountedEntity>.NewId();
        await _ctx.Transact(new CreateCountedEntity
        {
            Name = "Test",
            Id = newId,
            InitialCount = 0
        });
        var entity = await _ctx.Retrieve(newId);
        entity.Count.Should().Be(0);
        await _ctx.Transact(new IncrementCount {Entity = newId, Increment = 1});
        entity.Count.Should().Be(1);
        await _ctx.Transact(new IncrementCount {Entity = newId, Increment = 1});
        entity.Count.Should().Be(2);

        _ctx.ResetCache();
        entity = await _ctx.Retrieve(newId);
        entity.Count.Should().Be(2);

    }
}
