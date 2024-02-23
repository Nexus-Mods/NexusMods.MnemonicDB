using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class EntityIdSerializer : IValueSerializer<EntityId>
{
    public Type NativeType => typeof(EntityId);
    public Symbol UniqueId => Symbol.Intern<EntityIdSerializer>();
    public int Compare<TDatomA, TDatomB>(in TDatomA a, in TDatomB b) where TDatomA : IRawDatom where TDatomB : IRawDatom
    {
        return a.ValueLiteral.CompareTo(b.ValueLiteral);
    }

    public void Write<TWriter>(EntityId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out EntityId val)
    {
        throw new NotImplementedException();
    }

    public bool Serialize<TWriter>(EntityId value, TWriter buffer, out ulong valueLiteral) where TWriter : IBufferWriter<byte>
    {
        valueLiteral = value.Value;
        return true;
    }
}
