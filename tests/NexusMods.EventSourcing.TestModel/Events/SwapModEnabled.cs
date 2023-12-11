using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("8492075A-DED5-42BF-8D01-B4CDCE2526CF")]
[MemoryPackable]
public partial class SwapModEnabled : IEvent
{
    public required EntityId<Mod> Id { get; init; }
    public async ValueTask Apply<T>(T context) where T : IEventContext
    {
        var mod = await context.Retrieve(Id);
        mod.Enabled = !mod.Enabled;
    }

    /// <summary>
    /// Helper method to create a new event instance.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static SwapModEnabled Create(EntityId<Mod> id) => new() { Id = id };

    public void ModifiedEntities(Action<EntityId> handler)
    {
        handler(Id.Value);
    }
}
