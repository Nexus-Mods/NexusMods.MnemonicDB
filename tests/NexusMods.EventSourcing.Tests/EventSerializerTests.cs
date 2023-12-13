using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public class EventSerializerTests(EventSerializer serializer)
{

    [Fact]
    public void CanSerializeEvents()
    {
        serializer.Serialize(new SwapModEnabled(EntityId<Mod>.NewId(), true));
    }

    [Fact]
    public void CanDeserializeEvents()
    {
        var id = EntityId<Mod>.NewId();
        var @event = new SwapModEnabled(id, true);
        var serialized = serializer.Serialize(@event);
        var deserialized = serializer.Deserialize(serialized);
        deserialized.Should().BeEquivalentTo(@event);
    }
}
