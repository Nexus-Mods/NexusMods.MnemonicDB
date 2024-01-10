
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public class SerializationTests(BinaryEventSerializer serializer)
{

    [Fact]
    public void CanSerializeEvents()
    {

        var evnt = new SimpleTestEvent(420000, 112);
        var serialized = serializer.Serialize(evnt);

        var deserialized = serializer.Deserialize(serialized);
        deserialized.Should().Be(evnt);
    }

    [Theory]
    [MemberData(nameof(ExampleEvents))]
    public void CanSerializeAllEvents(IEvent @event)
    {
        var serialized = serializer.Serialize(@event);
        var deserialized = serializer.Deserialize(serialized);
        deserialized.Should()
            .BeEquivalentTo(@event, opts => opts.RespectingRuntimeTypes());
    }

    public static IEnumerable<object[]> ExampleEvents()
    {
        var events = new List<object[]>
        {
            new object[]{new CreateLoadout(EntityId<Loadout>.NewId(), "Test")},
            new object[]{new RenameLoadout(EntityId<Loadout>.NewId(), "Test")},
            new object[]{new AddMod("New Mod", true, EntityId<Mod>.NewId(), new EntityId<Loadout>())},
            new object[]{new AddCollection(EntityId<Collection>.NewId(), "NewCollection", EntityId<Loadout>.NewId(),
                [EntityId<Mod>.NewId()])
            },
            new object[]{new DeleteMod(EntityId<Mod>.NewId(), EntityId<Loadout>.NewId())},
            new object[]{new RenameLoadout(EntityId<Loadout>.NewId(), "Test")},
            new object[]{new SwapModEnabled(EntityId<Mod>.NewId(), true)},

        };
        return events;
    }


}
