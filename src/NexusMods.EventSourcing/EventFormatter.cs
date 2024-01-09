using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using MemoryPack.Formatters;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

internal class EventFormatter : MemoryPackFormatter<IEvent>
{
    private static Guid _zeroGuid = Guid.Empty;
    private readonly Dictionary<UInt128,Type> _eventByGuid;
    private readonly Dictionary<Type,UInt128> _eventsByType;

    public EventFormatter(IEnumerable<EventDefinition> events)
    {
        var eventsArray = events.ToArray();
       _eventByGuid = eventsArray.ToDictionary(e => e.Id, e => e.Type);
       _eventsByType = eventsArray.ToDictionary(e => e.Type, e => e.Id);
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
        throw new NotImplementedException();
        /*
        var readValue = reader.ReadValue<UInt128>();
        if (readValue == _zeroGuid)
        {
            value = null;
            return;
        }
        var mappedType = _eventByGuid[readValue];
        value = (IEvent)reader.ReadValue(mappedType)!;
        */
    }
}
