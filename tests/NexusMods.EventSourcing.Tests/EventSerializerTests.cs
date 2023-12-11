using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public class EventSerializerTests(EventSerializer serializer)
{

    [Fact]
    public void CanSerializeEvents()
    {
        serializer.Serialize(SwapModEnabled.Create(EntityId<Mod>.NewId()));
    }

    [Fact]
    public void CanDeserializeEvents()
    {
        var id = EntityId<Mod>.NewId();
        var @event = SwapModEnabled.Create(id);
        var serialized = serializer.Serialize(@event);
        var deserialized = serializer.Deserialize(serialized);
        deserialized.Should().BeEquivalentTo(@event);
    }
}
