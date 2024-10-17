using System;
using System.Buffers;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.ValueSerializers;

/// <summary>
/// A serializer for the Null value
/// </summary>
public class NullSerializer : IValueSerializer<Null>
{
    public static ValueTag ValueTag => ValueTag.Null;
    
    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b) => 0;

    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return 0;
    }

    /// <inheritdoc />
    public static Null Read(ReadOnlySpan<byte> span)
    {
        return Null.Instance;
    }

    /// <inheritdoc />
    public static void Write<TWriter>(Null value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        // Do nothing
    }

    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // Do nothing
    }
}
