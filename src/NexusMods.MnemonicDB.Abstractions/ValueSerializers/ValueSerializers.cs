﻿    using System;
    using System.Buffers;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using NexusMods.MnemonicDB.Abstractions;
    using NexusMods.MnemonicDB.Abstractions.ElementComparers;
namespace NexusMods.MnemonicDB.Abstractions.ValueSerializers;


/// <summary>
/// The serializer for the UInt8 type
/// </summary>
public sealed class UInt8Serializer : IValueSerializer<byte>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.UInt8;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((byte*)aPtr)).CompareTo(*((byte*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<byte>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte Read(byte* ptr, int len)
    {
        return *((byte*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(byte value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(byte));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(byte));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the UInt16 type
/// </summary>
public sealed class UInt16Serializer : IValueSerializer<ushort>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.UInt16;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((ushort*)aPtr)).CompareTo(*((ushort*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<ushort>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ushort Read(byte* ptr, int len)
    {
        return *((ushort*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(ushort value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(ushort));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(ushort));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the UInt32 type
/// </summary>
public sealed class UInt32Serializer : IValueSerializer<uint>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.UInt32;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((uint*)aPtr)).CompareTo(*((uint*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<uint>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint Read(byte* ptr, int len)
    {
        return *((uint*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(uint value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(uint));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(uint));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the UInt64 type
/// </summary>
public sealed class UInt64Serializer : IValueSerializer<ulong>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.UInt64;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((ulong*)aPtr)).CompareTo(*((ulong*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<ulong>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong Read(byte* ptr, int len)
    {
        return *((ulong*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(ulong value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(ulong));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(ulong));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the UInt128 type
/// </summary>
public sealed class UInt128Serializer : IValueSerializer<UInt128>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.UInt128;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((UInt128*)aPtr)).CompareTo(*((UInt128*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt128 Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<UInt128>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe UInt128 Read(byte* ptr, int len)
    {
        return *((UInt128*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(UInt128 value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(UInt128));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(UInt128));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the Int16 type
/// </summary>
public sealed class Int16Serializer : IValueSerializer<short>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Int16;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((short*)aPtr)).CompareTo(*((short*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<short>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe short Read(byte* ptr, int len)
    {
        return *((short*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(short value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(short));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(short));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the Int32 type
/// </summary>
public sealed class Int32Serializer : IValueSerializer<int>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Int32;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((int*)aPtr)).CompareTo(*((int*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<int>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int Read(byte* ptr, int len)
    {
        return *((int*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(int value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(int));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(int));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the Int64 type
/// </summary>
public sealed class Int64Serializer : IValueSerializer<long>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Int64;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((long*)aPtr)).CompareTo(*((long*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<long>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe long Read(byte* ptr, int len)
    {
        return *((long*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(long value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(long));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(long));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the Int128 type
/// </summary>
public sealed class Int128Serializer : IValueSerializer<Int128>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Int128;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((Int128*)aPtr)).CompareTo(*((Int128*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int128 Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<Int128>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Int128 Read(byte* ptr, int len)
    {
        return *((Int128*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(Int128 value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(Int128));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(Int128));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the Float32 type
/// </summary>
public sealed class Float32Serializer : IValueSerializer<float>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Float32;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((float*)aPtr)).CompareTo(*((float*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<float>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe float Read(byte* ptr, int len)
    {
        return *((float*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(float value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(float));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(float));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}


/// <summary>
/// The serializer for the Float64 type
/// </summary>
public sealed class Float64Serializer : IValueSerializer<double>
{
    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Float64;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Read(a).CompareTo(Read(b));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return (*((double*)aPtr)).CompareTo(*((double*)bPtr));
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<double>(span);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe double Read(byte* ptr, int len)
    {
        return *((double*)ptr);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(double value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        unsafe {
            var span = writer.GetSpan(sizeof(double));
            MemoryMarshal.Write(span, value);
            writer.Advance(sizeof(double));
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        // No-op
    }

}

