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

    #endregion

}
