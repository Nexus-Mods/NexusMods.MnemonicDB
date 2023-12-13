using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using MemoryPack.Formatters;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class EventFormatter : MemoryPackFormatter<IEvent>
{
    private static Guid _zeroGuid = Guid.Empty;
    private readonly Dictionary<Guid,Type> _eventByGuid;
    private readonly Dictionary<Type,Guid> _eventsByType;

    public EventFormatter(IEnumerable<EventDefinition> events)
    {
        var eventsArray = events.ToArray();
       _eventByGuid = eventsArray.ToDictionary(e => e.Guid, e => e.Type);
       _eventsByType = eventsArray.ToDictionary(e => e.Type, e => e.Guid);
    }

    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref IEvent? value)
    {
        if (value == null)
        {
            writer.WriteValue(_zeroGuid);
            return;
        }

        var type = value.GetType();
        writer.WriteValue(_eventsByType[type]);
        writer.WriteValue(type, value);
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref IEvent? value)
    {
        var readValue = reader.ReadValue<Guid>();
        if (readValue == _zeroGuid)
        {
            value = null;
            return;
        }
        var mappedType = _eventByGuid[readValue];
        value = (IEvent)reader.ReadValue(mappedType)!;
    }
}
