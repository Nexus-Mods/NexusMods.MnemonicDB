using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.ValueSerializers;

/// <summary>
/// A value serializer for <see cref="EntityId"/>.
/// </summary>
public class EntityIdSerializer : IValueSerializer<EntityId>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Reference;
    
    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<EntityId>(a).CompareTo(MemoryMarshal.Read<EntityId>(b));
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*(EntityId*)aPtr).CompareTo(*(EntityId*)bPtr);
    }

    /// <inheritdoc />
    public static EntityId Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<EntityId>(span);
    }
    
    /// <inheritdoc />
    public static unsafe void Write<TWriter>(EntityId value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(sizeof(EntityId));
        MemoryMarshal.Write(span, value);
        writer.Advance(sizeof(EntityId));
    }
    
    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        MemoryMarshal.Write(span, remapFn(MemoryMarshal.Read<EntityId>(span)));
    }
}
