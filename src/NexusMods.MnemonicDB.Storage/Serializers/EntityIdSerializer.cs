using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

/// <inheritdoc />
public class EntityIdSerializer : IValueSerializer<EntityId>
{
    /// <inheritdoc />
    public Type NativeType => typeof(EntityId);

    /// <inheritdoc />
    public LowLevelTypes LowLevelType => LowLevelTypes.Reference;

    /// <inheritdoc />
    public Symbol UniqueId => Symbol.Intern<EntityIdSerializer>();

    public EntityId Read(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        Debug.Assert(prefix is { LowLevelType: LowLevelTypes.UInt, ValueLength: sizeof(ulong) });
        return EntityId.From(MemoryMarshal.Read<ulong>(valueSpan));
    }

    public void Serialize<TWriter>(ref KeyPrefix prefix, EntityId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        prefix.ValueLength = sizeof(ulong);
        prefix.LowLevelType = LowLevelTypes.Reference;
        var span = buffer.GetSpan(KeyPrefix.Size + sizeof(ulong));
        MemoryMarshal.Write(span, prefix);
        MemoryMarshal.Write(span.SliceFast(KeyPrefix.Size), value);
        buffer.Advance(KeyPrefix.Size + sizeof(ulong));
    }
}
