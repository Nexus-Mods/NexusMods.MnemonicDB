using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

/// <inheritdoc />
public class EntityIdSerializer : IValueSerializer<EntityId>
{
    /// <inheritdoc />
    public Type NativeType => typeof(EntityId);

    /// <inheritdoc />
    public Symbol UniqueId => Symbol.Intern<EntityIdSerializer>();

    /// <inheritdoc />
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<EntityId>(a).CompareTo(MemoryMarshal.Read<EntityId>(b));
    }

    /// <inheritdoc />
    public EntityId Read(ReadOnlySpan<byte> buffer)
    {
        return MemoryMarshal.Read<EntityId>(buffer);
    }

    /// <inheritdoc />
    public void Serialize<TWriter>(EntityId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(sizeof(ulong));
        MemoryMarshal.Write(span, value.Value);
        buffer.Advance(sizeof(ulong));
    }
}
