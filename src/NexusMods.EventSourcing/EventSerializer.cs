using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemoryPack;
using MemoryPack.Formatters;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class EventSerializer
{
    public EventSerializer(IEnumerable<EventDefinition> events)
    {
        var formatter = new EventFormatter(events);
        if (!MemoryPackFormatterProvider.IsRegistered<IEvent>())
            MemoryPackFormatterProvider.Register(formatter);
    }

    public byte[] Serialize(IEvent @event)
    {
        return MemoryPackSerializer.Serialize(@event);
    }

    public IEvent Deserialize(byte[] data)
    {
        return MemoryPackSerializer.Deserialize<IEvent>(data)!;
    }

}
