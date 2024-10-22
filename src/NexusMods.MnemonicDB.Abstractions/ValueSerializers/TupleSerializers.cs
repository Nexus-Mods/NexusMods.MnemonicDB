using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.ValueSerializers;

/// <summary>
/// A value serializer for a tuple of <see cref="ushort"/> and case-insensitive <see cref="string"/>.
/// </summary>
public sealed class Tuple2_UShort_Utf8I_Serializer : IValueSerializer<(ushort, string)>
{
    public static ValueTag ValueTag => ValueTag.Tuple2_UShort_Utf8I;
    
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var aUShort = MemoryMarshal.Read<ushort>(a);
        var bUShort = MemoryMarshal.Read<ushort>(b);
        var ushortComparison = aUShort.CompareTo(bUShort);
        if (ushortComparison != 0) return ushortComparison;

        return Utf8InsensitiveSerializer.Compare(a.SliceFast(sizeof(ushort)), b.SliceFast(sizeof(ushort)));
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var aUShort = *(ushort*)aVal;
        var bUShort = *(ushort*)bVal;
        var ushortComparison = aUShort.CompareTo(bUShort);
        if (ushortComparison != 0) return ushortComparison;

        return Utf8InsensitiveSerializer.Compare(aVal + sizeof(ushort), aLen - sizeof(ushort), bVal + sizeof(ushort), bLen - sizeof(ushort));  
    }

    /// <inheritdoc />
    public static (ushort, string) Read(ReadOnlySpan<byte> span)
    {
        var item1 = MemoryMarshal.Read<ushort>(span);
        var item2 = Encoding.UTF8.GetString(span.SliceFast(sizeof(ushort)).ToArray());
        return (item1, item2);
    }

    /// <inheritdoc />
    public static void Write<TWriter>((ushort, string) value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(sizeof(ushort) + Encoding.UTF8.GetByteCount(value.Item2));
        MemoryMarshal.Write(span, value.Item1);
        Encoding.UTF8.GetBytes(value.Item2, span.SliceFast(sizeof(ushort)));
        writer.Advance(sizeof(ushort) + Encoding.UTF8.GetByteCount(value.Item2));
    }

    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
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
    public static unsafe int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var aEntityId = MemoryMarshal.Read<EntityId>(a);
        var bEntityId = MemoryMarshal.Read<EntityId>(b);
        var entityIdComparison = aEntityId.CompareTo(bEntityId);
        if (entityIdComparison != 0) return entityIdComparison;

        var aUShort = MemoryMarshal.Read<ushort>(a.SliceFast(sizeof(EntityId)));
        var bUShort = MemoryMarshal.Read<ushort>(b.SliceFast(sizeof(EntityId)));
        var ushortComparison = aUShort.CompareTo(bUShort);
        if (ushortComparison != 0) return ushortComparison;

        return Utf8InsensitiveSerializer.Compare(a.SliceFast(sizeof(EntityId) + sizeof(ushort)), b.SliceFast(sizeof(EntityId) + sizeof(ushort)));
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

        var offset = sizeof(EntityId) + sizeof(ushort);
        return Utf8InsensitiveSerializer.Compare(aVal + offset, aLen - offset, bVal + offset, bLen - offset);
    }

    /// <inheritdoc />
    public static unsafe (EntityId, ushort, string) Read(ReadOnlySpan<byte> span)
    {
        var item1 = MemoryMarshal.Read<EntityId>(span);
        var item2 = MemoryMarshal.Read<ushort>(span.SliceFast(sizeof(EntityId)));
        var item3 = Encoding.UTF8.GetString(span.SliceFast(sizeof(EntityId) + sizeof(ushort)).ToArray());
        return (item1, item2, item3);
    }

    /// <inheritdoc />
    public static unsafe void Write<TWriter>((EntityId, ushort, string) value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(sizeof(EntityId) + sizeof(ushort) + Encoding.UTF8.GetByteCount(value.Item3));
        MemoryMarshal.Write(span, value.Item1);
        MemoryMarshal.Write(span.SliceFast(sizeof(EntityId)), value.Item2);
        Encoding.UTF8.GetBytes(value.Item3, span.SliceFast(sizeof(EntityId) + sizeof(ushort)));
        writer.Advance(sizeof(EntityId) + sizeof(ushort) + Encoding.UTF8.GetByteCount(value.Item3));
    }

    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        MemoryMarshal.Write(span, remapFn(MemoryMarshal.Read<EntityId>(span)));
    }
}
