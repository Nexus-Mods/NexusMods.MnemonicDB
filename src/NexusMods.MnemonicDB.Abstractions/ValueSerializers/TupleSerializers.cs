using System;
using System.Buffers;
using System.Text;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.ValueSerializers;

/// <summary>
/// A value serializer for a tuple of <see cref="ushort"/> and case-insensitive <see cref="string"/>.
/// </summary>
public sealed class Tuple2_UShort_Utf8I_Serializer : IValueSerializer<(ushort, string)>
{
    public static ValueTag ValueTag => ValueTag.Tuple2_UShort_Utf8I;
    
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var aUShort = *(ushort*)aVal;
        var bUShort = *(ushort*)bVal;
        var ushortComparison = aUShort.CompareTo(bUShort);
        if (ushortComparison != 0) return ushortComparison;

        var aStr = Encoding.UTF8.GetString(aVal + sizeof(ushort), aLen - sizeof(ushort));
        var bStr = Encoding.UTF8.GetString(bVal + sizeof(ushort), bLen - sizeof(ushort));
        return string.Compare(aStr, bStr, StringComparison.Ordinal);    
    }

    public static (ushort, string) Read(ReadOnlySpan<byte> span)
    {
        throw new NotImplementedException();
    }

    public static void Write<TWriter>((ushort, string) value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// A value serializer for a tuple of <see cref="EntityId"/>, <see cref="ushort"/>, and case-insensitive <see cref="string"/>.
/// </summary>
public sealed class Tuple3_Ref_UShort_Utf8I_Serializer : IValueSerializer<(EntityId, ushort, string)>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Tuple3_Ref_UShort_Utf8I;

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var aEntityId = *(EntityId*)aVal;
        var bEntityId = *(EntityId*)bVal;
        var entityIdComparison = aEntityId.CompareTo(bEntityId);
        if (entityIdComparison != 0) return entityIdComparison;

        var aUShort = *(ushort*)(aVal + sizeof(EntityId));
        var bUShort = *(ushort*)(bVal + sizeof(EntityId));
        var ushortComparison = aUShort.CompareTo(bUShort);
        if (ushortComparison != 0) return ushortComparison;

        var aStr = Encoding.UTF8.GetString(aVal + sizeof(EntityId) + sizeof(ushort), aLen - sizeof(EntityId) - sizeof(ushort));
        var bStr = Encoding.UTF8.GetString(bVal + sizeof(EntityId) + sizeof(ushort), bLen - sizeof(EntityId) - sizeof(ushort));
        return string.Compare(aStr, bStr, StringComparison.Ordinal);
    }

    public static (EntityId, ushort, string) Read(ReadOnlySpan<byte> span)
    {
        throw new NotImplementedException();
    }

    public static void Write<TWriter>((EntityId, ushort, string) value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        throw new NotImplementedException();
    }
}
