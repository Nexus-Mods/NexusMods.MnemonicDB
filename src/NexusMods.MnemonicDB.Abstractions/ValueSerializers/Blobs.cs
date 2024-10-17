using System;
using System.Buffers;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.ValueSerializers;

/// <summary>
/// A serializer for blobs, which are chunks of memory
/// </summary>
public sealed class BlobSerializer : IValueSerializer<Memory<byte>>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Blob;

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return new ReadOnlySpan<byte>(aPtr, aLen).SequenceCompareTo(new ReadOnlySpan<byte>(bPtr, bLen));
    }

    /// <inheritdoc />
    public static Memory<byte> Read(ReadOnlySpan<byte> span)
    {
        return span.ToArray();
    }

    /// <inheritdoc />
    public static void Write<TWriter>(Memory<byte> value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(value.Length);
        value.Span.CopyTo(span);
        writer.Advance(value.Length);
    }

    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
    }
}


/// <summary>
/// A serializer for hashed blobs, which are chunks of memory that have been hashed so that only the hash is used
/// during comparisons
/// </summary>
public sealed class HashedBlobSerializer : IValueSerializer<Memory<byte>>
{
    
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.HashedBlob;

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var aHash = MemoryMarshal.Read<ulong>(a);
        var bHash = MemoryMarshal.Read<ulong>(b);
        return aHash.CompareTo(bHash);
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var aHash = *(ulong*)aPtr;
        var bHash = *(ulong*)bPtr;
        return aHash.CompareTo(bHash);
    }

    /// <inheritdoc />
    public static Memory<byte> Read(ReadOnlySpan<byte> span)
    {
        return span.SliceFast(sizeof(ulong)).ToArray();
    }

    /// <inheritdoc />
    public static void Write<TWriter>(Memory<byte> value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(sizeof(ulong) + value.Length);
        var hash = XxHash3.HashToUInt64(value.Span);
        MemoryMarshal.Write(span, hash);
        value.Span.CopyTo(span.SliceFast(sizeof(ulong)));
    }

    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
    }
}
