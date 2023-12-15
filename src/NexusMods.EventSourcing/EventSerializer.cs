using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemoryPack;
using MemoryPack.Formatters;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class EventSerializer : IEventSerializer
{
    private readonly PooledMemoryBufferWriter _writer;

    public EventSerializer(IEnumerable<EventDefinition> events)
    {
        var formatter = new EventFormatter(events);
        if (!MemoryPackFormatterProvider.IsRegistered<IEvent>())
            MemoryPackFormatterProvider.Register(formatter);
        _writer = new PooledMemoryBufferWriter(1024 * 10); // 10kb
    }

    public ReadOnlySpan<byte> Serialize(IEvent @event)
    {
        _writer.Reset();
        MemoryPackSerializer.Serialize(_writer, @event);
        return _writer.GetWrittenSpan();
    }

    public IEvent Deserialize(ReadOnlySpan<byte> data)
    {
        return MemoryPackSerializer.Deserialize<IEvent>(data)!;
    }



}
