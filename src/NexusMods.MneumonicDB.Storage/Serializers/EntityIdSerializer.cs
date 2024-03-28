using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions;

namespace NexusMods.MneumonicDB.Storage.Serializers;

public class EntityIdSerializer : IValueSerializer<EntityId>
{
    public Type NativeType => typeof(EntityId);
    public Symbol UniqueId => Symbol.Intern<EntityIdSerializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<EntityId>(a).CompareTo(MemoryMarshal.Read<EntityId>(b));
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
