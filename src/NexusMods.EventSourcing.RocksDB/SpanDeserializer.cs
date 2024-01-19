using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.RocksDB;

/// <summary>
/// A deserializer for RocksDB events, used so we can deserializer during read events
/// </summary>
/// <param name="serializer"></param>
/// <typeparam name="TSerializer"></typeparam>
public class SpanDeserializer<TSerializer>(TSerializer serializer) : ISpanDeserializer<IEvent>
    where TSerializer : IEventSerializer
{
    /// <inheritdoc />
    public IEvent Deserialize(ReadOnlySpan<byte> buffer)
    {
        return serializer.Deserialize(buffer);
    }
}
