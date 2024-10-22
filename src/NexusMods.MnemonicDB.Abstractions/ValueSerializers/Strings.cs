using System;
using System.Buffers;
using System.Text;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.ValueSerializers;

/// <summary>
/// A value serializer for a utf8 string
/// </summary>
public class Utf8Serializer : IValueSerializer<string>
{
    /// <summary>
    /// Encoding to use for the string
    /// </summary>
    private static readonly Encoding Encoding = Encoding.UTF8;

    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Utf8;
    
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
    public static string Read(ReadOnlySpan<byte> span)
    {
        return Encoding.GetString(span);
    }

    /// <inheritdoc />
    public static void Write<TWriter>(string value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(Encoding.GetMaxByteCount(value.Length));
        var bytesWritten = Encoding.GetBytes(value, span);
        writer.Advance(bytesWritten);
    }

    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        
    }
}


/// <summary>
/// A value serializer for a utf8 string
/// </summary>
public class AsciiSerializer : IValueSerializer<string>
{
    /// <summary>
    /// Encoding to use for the string
    /// </summary>
    private static readonly Encoding Encoding = Encoding.ASCII;

    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Ascii;
    
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
    public static string Read(ReadOnlySpan<byte> span)
    {
        return Encoding.GetString(span);
    }

    /// <inheritdoc />
    public static void Write<TWriter>(string value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(Encoding.GetMaxByteCount(value.Length));
        var bytesWritten = Encoding.GetBytes(value, span);
        writer.Advance(bytesWritten);
    }

    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        
    }
}

/// <summary>
/// A value serializer for a utf8 string
/// </summary>
public class Utf8InsensitiveSerializer : IValueSerializer<string>
{
    /// <summary>
    /// Encoding to use for the string
    /// </summary>
    private static readonly Encoding Encoding = Encoding.UTF8;

    /// <inheritdoc />
    public static ValueTag ValueTag => ValueTag.Utf8Insensitive;
    
    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        unsafe
        {
            fixed (byte* aPtr = a)
            fixed (byte* bPtr = b)
            {
                return Utf8Comparer.Utf8CaseInsensitiveCompare(aPtr, a.Length, bPtr, b.Length);
            }
        }
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return Utf8Comparer.Utf8CaseInsensitiveCompare(aPtr, aLen, bPtr, bLen);
    }

    /// <inheritdoc />
    public static string Read(ReadOnlySpan<byte> span)
    {
        return Encoding.GetString(span);
    }

    /// <inheritdoc />
    public static void Write<TWriter>(string value, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var span = writer.GetSpan(Encoding.GetMaxByteCount(value.Length));
        var bytesWritten = Encoding.GetBytes(value, span);
        writer.Advance(bytesWritten);
    }

    /// <inheritdoc />
    public static void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        
    }
}
