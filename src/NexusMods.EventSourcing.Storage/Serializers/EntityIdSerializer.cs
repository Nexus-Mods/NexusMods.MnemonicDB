using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class EntityIdSerializer : IValueSerializer<EntityId>
{
    public Type NativeType => typeof(EntityId);
    public Symbol UniqueId => Symbol.Intern<EntityIdSerializer>();
    public int Compare(in Datom a, in Datom b)
    {
        return a.Unmarshal<EntityId>().CompareTo(b.Unmarshal<EntityId>());
    }

    public void Write<TWriter>(EntityId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out EntityId val)
    {
        val = MemoryMarshal.Read<EntityId>(buffer);
        return sizeof(ulong);
    }

    public void Serialize<TWriter>(EntityId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(sizeof(ulong));
        MemoryMarshal.Write(span, value.Value);
        buffer.Advance(sizeof(ulong));
    }
}
