using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

public static class ValueSerializer
{
    #region Constants
    private const int MaxStackAlloc = 128;
    private static readonly Encoding AsciiEncoding = Encoding.ASCII;
    private static readonly Encoding Utf8Encoding = Encoding.UTF8;



    #endregion

    #region Writers

    /// <summary>
    /// Writes a null value to the buffer
    /// </summary>
    public static void WriteNull<TWriter>(TWriter writer) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.Null);
    }

    /// <summary>
    /// Writes an 8-bit unsigned integer to the buffer
    /// </summary>
    public static void WriteUInt8<TWriter>(TWriter writer, byte value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.UInt8);
        Span<byte> span = stackalloc byte[1];
        span[0] = value;
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 16-bit unsigned integer to the buffer
    /// </summary>
    public static void WriteUInt16<TWriter>(TWriter writer, ushort value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.UInt16);
        Span<byte> span = stackalloc byte[2];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 32-bit unsigned integer to the buffer
    /// </summary>
    public static void WriteUInt32<TWriter>(TWriter writer, uint value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.UInt32);
        Span<byte> span = stackalloc byte[4];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 64-bit unsigned integer to the buffer
    /// </summary>
    public static void WriteUInt64<TWriter>(TWriter writer, ulong value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.UInt64);
        Span<byte> span = stackalloc byte[8];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 128-bit unsigned integer to the buffer
    /// </summary>
    public static void WriteUInt128<TWriter>(TWriter writer, UInt128 value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.UInt128);
        Span<byte> span = stackalloc byte[16];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }


    /// <summary>
    /// Writes a 16-bit signed integer to the buffer
    /// </summary>
    public static void WriteInt16<TWriter>(TWriter writer, short value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.Int16);
        Span<byte> span = stackalloc byte[2];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 32-bit signed integer to the buffer
    /// </summary>
    public static void WriteInt32<TWriter>(TWriter writer, int value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.Int32);
        Span<byte> span = stackalloc byte[4];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 64-bit signed integer to the buffer
    /// </summary>
    public static void WriteInt64<TWriter>(TWriter writer, long value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.Int64);
        Span<byte> span = stackalloc byte[8];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 128-bit signed integer to the buffer
    /// </summary>
    public static void WriteInt128<TWriter>(TWriter writer, Int128 value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.Int128);
        Span<byte> span = stackalloc byte[16];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 32-bit floating point number to the buffer
    /// </summary>
    public static void WriteFloat32<TWriter>(TWriter writer, float value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.Float32);
        Span<byte> span = stackalloc byte[4];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a 64-bit floating point number to the buffer
    /// </summary>
    public static void WriteFloat64<TWriter>(TWriter writer, double value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.Float64);
        Span<byte> span = stackalloc byte[8];
        MemoryMarshal.Write(span, value);
        writer.Write(span);
    }

    /// <summary>
    /// Writes an ASCII string to the buffer
    /// </summary>
    public static void WriteAscii<TWriter>(TWriter writer, ReadOnlySpan<char> value) where TWriter
        : IBufferWriter<byte>
    {
        var size = AsciiEncoding.GetByteCount(value);
        var span = size <= MaxStackAlloc ? stackalloc byte[size] : GC.AllocateUninitializedArray<byte>(size);

        WriteTag(writer, ValueTags.Ascii);
        AsciiEncoding.GetBytes(value, span);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a UTF-8 string to the buffer, with the specified sensitivity mode
    /// </summary>
    public static void WriteUtf8<TWriter>(TWriter writer, ReadOnlySpan<char> value, bool caseSensitive = true) where TWriter
        : IBufferWriter<byte>
    {
        var size = Utf8Encoding.GetByteCount(value);
        var span = size <= MaxStackAlloc ? stackalloc byte[size] : GC.AllocateUninitializedArray<byte>(size);

        WriteTag(writer, caseSensitive ? ValueTags.Utf8 : ValueTags.Utf8Insensitive);
        Utf8Encoding.GetBytes(value, span);
        writer.Write(span);
    }

    /// <summary>
    /// Writes a blob to the buffer
    /// </summary>
    public static void WriteBlob<TWriter>(TWriter writer, ReadOnlySpan<byte> value) where TWriter
        : IBufferWriter<byte>
    {
        WriteTag(writer, ValueTags.Blob);
        writer.Write(value);
    }
    #endregion


    #region Readers

    /// <summary>
    /// "Reads" a null value from the buffer, really just throws
    /// and exception if the tag is not ValueTags.Null
    /// </summary>
    public static void GetNull(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Null)
            ThrowTagException(ValueTags.Null, tag);
    }

    /// <summary>
    /// Reads a UInt8 value from the buffer
    /// </summary>
    public static byte GetUInt8(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.UInt8)
            ThrowTagException(ValueTags.UInt8, tag);
        var value = span[0];
        span = span.SliceFast(1);
        return value;
    }

    /// <summary>
    /// Reads a UInt16 value from the buffer
    /// </summary>
    public static ushort GetUInt16(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.UInt16)
            ThrowTagException(ValueTags.UInt16, tag);
        var value = MemoryMarshal.Read<ushort>(span);
        span = span.SliceFast(2);
        return value;
    }

    /// <summary>
    /// Reads a UInt32 value from the buffer
    /// </summary>
    public static uint GetUInt32(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.UInt32)
            ThrowTagException(ValueTags.UInt32, tag);
        var value = MemoryMarshal.Read<uint>(span);
        span = span.SliceFast(4);
        return value;
    }

    /// <summary>
    /// Reads a UInt64 value from the buffer
    /// </summary>
    public static ulong GetUInt64(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.UInt64)
            ThrowTagException(ValueTags.UInt64, tag);
        var value = MemoryMarshal.Read<ulong>(span);
        span = span.SliceFast(8);
        return value;
    }

    /// <summary>
    /// Reads a UInt128 value from the buffer
    /// </summary>
    public static UInt128 GetUInt128(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.UInt128)
            ThrowTagException(ValueTags.UInt128, tag);
        var value = MemoryMarshal.Read<UInt128>(span);
        span = span.SliceFast(16);
        return value;
    }

    /// <summary>
    /// Reads a Int16 value from the buffer
    /// </summary>
    public static short GetInt16(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Int16)
            ThrowTagException(ValueTags.Int16, tag);
        var value = MemoryMarshal.Read<short>(span);
        span = span.SliceFast(2);
        return value;
    }

    /// <summary>
    /// Reads an Int32 value from the buffer
    /// </summary>
    public static int GetInt32(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Int32)
            ThrowTagException(ValueTags.Int32, tag);
        var value = MemoryMarshal.Read<int>(span);
        span = span.SliceFast(4);
        return value;
    }

    /// <summary>
    /// Reads an Int64 value from the buffer
    /// </summary>
    public static long GetInt64(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Int64)
            ThrowTagException(ValueTags.Int64, tag);
        var value = MemoryMarshal.Read<long>(span);
        span = span.SliceFast(8);
        return value;
    }

    /// <summary>
    /// Reads an Int128 value from the buffer
    /// </summary>
    public static Int128 GetInt128(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Int128)
            ThrowTagException(ValueTags.Int128, tag);
        var value = MemoryMarshal.Read<Int128>(span);
        span = span.SliceFast(16);
        return value;
    }

    /// <summary>
    /// Reads a Float32 value from the buffer
    /// </summary>
    public static float GetFloat32(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Float32)
            ThrowTagException(ValueTags.Float32, tag);
        var value = MemoryMarshal.Read<float>(span);
        span = span.SliceFast(4);
        return value;
    }

    /// <summary>
    /// Reads a Float64 value from the buffer
    /// </summary>
    public static double GetFloat64(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Float64)
            ThrowTagException(ValueTags.Float64, tag);
        var value = MemoryMarshal.Read<double>(span);
        span = span.SliceFast(8);
        return value;
    }

    /// <summary>
    /// Reads an Ascii string from the buffer
    /// </summary>
    public static string GetAscii(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Ascii)
            ThrowTagException(ValueTags.Ascii, tag);
        var value = AsciiEncoding.GetString(span);
        span = span.SliceFast(value.Length);
        return value;
    }

    /// <summary>
    /// Reads a UTF-8 string from the buffer
    /// </summary>
    public static string GetUtf8(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Utf8 && tag != ValueTags.Utf8Insensitive)
            ThrowTagException(ValueTags.Utf8, tag);
        var value = Utf8Encoding.GetString(span);
        span = span.SliceFast(value.Length);
        return value;
    }

    /// <summary>
    /// Reads a blob from the buffer
    /// </summary>
    public static ReadOnlySpan<byte> GetBlob(ref ReadOnlySpan<byte> span)
    {
        var tag = ReadTag(ref span);
        if (tag != ValueTags.Blob)
            ThrowTagException(ValueTags.Blob, tag);
        var length = span.Length;
        span = span.SliceFast(length);
        return span;
    }

    /// <summary>
    /// Gets the value as a byte span
    /// </summary>
    /// <param name="p0"></param>
    /// <returns></returns>
    public static ReadOnlySpan<byte> GetRaw(ref ReadOnlySpan<byte> p0)
    {
        return p0.SliceFast(1);
    }

    #endregion


    #region Dispatchers

    /// <summary>
    /// Writes a value to the buffer
    /// </summary>
    public static void Write<T, TWriter>(TWriter writer, T value)
        where TWriter : IBufferWriter<byte>
    {
        switch (value)
        {
            case byte v:
                WriteUInt8(writer, v);
                break;
            case ushort v:
                WriteUInt16(writer, v);
                break;
            case uint v:
                WriteUInt32(writer, v);
                break;
            case ulong v:
                WriteUInt64(writer, v);
                break;
            case UInt128 v:
                WriteUInt128(writer, v);
                break;
            case short v:
                WriteInt16(writer, v);
                break;
            case int v:
                WriteInt32(writer, v);
                break;
            case long v:
                WriteInt64(writer, v);
                break;
            case Int128 v:
                WriteInt128(writer, v);
                break;
            case float v:
                WriteFloat32(writer, v);
                break;
            case double v:
                WriteFloat64(writer, v);
                break;
            case string v:
                WriteUtf8(writer, v.AsSpan());
                break;
            default:
                throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }
    }

    #endregion


    #region Internal Helpers
    /// <summary>
    /// Writes the value tag to the buffer
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTag<TWriter>(TWriter writer, ValueTags tag) where TWriter
        : IBufferWriter<byte>
    {
        Span<byte> span = stackalloc byte[1];
        span[0] = (byte)tag;
        writer.Write(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTags ReadTag(ref ReadOnlySpan<byte> span)
    {
        var tag = (ValueTags)span[0];
        span = span.SliceFast(1);
        return tag;
    }


    /// <summary>
    /// Throws an exception for an unexpected tag
    /// </summary>
    private static void ThrowTagException(ValueTags expected, ValueTags actual)
    {
        throw new InvalidOperationException($"Expected tag {expected}, got {actual}");
    }

    #endregion


}
