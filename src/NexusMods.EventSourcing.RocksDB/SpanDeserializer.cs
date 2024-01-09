using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.RocksDB;

public class SpanDeserializer<TSerializer>(TSerializer serializer) : ISpanDeserializer<IEvent>
    where TSerializer : IEventSerializer
{
    public IEvent Deserialize(ReadOnlySpan<byte> buffer)
    {
        return serializer.Deserialize(buffer);
    }
}
