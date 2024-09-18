using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Exceptions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

public partial class Attribute<TValueType, TLowLevelType>
{
    /// <summary>
    /// Writes a value of a specific type to the given writer
    /// </summary>
    public virtual void WriteValue<TWriter>(TValueType value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        WriteValueLowLevel(ToLowLevel(value), LowLevelType, writer);
    }
    
    /// <summary>
    /// Writes a value of a specific type to the given writer
    /// </summary>
    public static void WriteValueLowLevel<TValue, TWriter>(TValue value, ValueTags tag, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        switch (value)
        {
            case Null:
                WriteNull(writer);
                break;
            case byte val:
                WriteUnmanaged(val, writer);
                break;
            case ushort val:
                WriteUnmanaged(val, writer);
                break;
            case uint val:
                WriteUnmanaged(val, writer);
                break;
            case ulong val:
                WriteUnmanaged(val, writer);
                break;
            case UInt128 val:
                WriteUnmanaged(val, writer);
                break;
            case short val:
                WriteUnmanaged(val, writer);
                break;
            case int val:
                WriteUnmanaged(val, writer);
                break;
            case long val:
                WriteUnmanaged(val, writer);
                break;
            case Int128 val:
                WriteUnmanaged(val, writer);
                break;
            case float val:
                WriteUnmanaged(val, writer);
                break;
            case double val:
                WriteUnmanaged(val, writer);
                break;
            case string s when tag == ValueTags.Ascii:
                WriteAscii(s, writer);
                break;
            case string s when tag == ValueTags.Utf8:
                WriteUtf8(s, writer);
                break;
            case string s when tag == ValueTags.Utf8Insensitive:
                WriteUtf8(s, writer);
                break;
            case EntityId val when tag == ValueTags.Reference:
                WriteUnmanaged(val, writer);
                break;
            default:
                throw new UnsupportedLowLevelWriteType<TValue>(value);
        }
    }

    /// <summary>
    /// Gets the size of a value of a specific type
    /// </summary>
    protected static int GetValueSize<T>(T type, ValueTags tag)
    {
        return tag switch
        {
            ValueTags.Null => 0,
            ValueTags.UInt8 => 1,
            ValueTags.UInt16 => 2,
            ValueTags.UInt32 => 4,
            ValueTags.UInt64 => 8,
            ValueTags.UInt128 => 16,
            ValueTags.Int16 => 2,
            ValueTags.Int32 => 4,
            ValueTags.Int64 => 8,
            ValueTags.Int128 => 16,
            ValueTags.Float32 => 4,
            ValueTags.Float64 => 8,
            ValueTags.Ascii => AsciiEncoding.GetByteCount((string)(object)type!) + sizeof(uint),
            ValueTags.Utf8 => Utf8Encoding.GetByteCount((string)(object)type!) + sizeof(uint),
            ValueTags.Utf8Insensitive => Utf8Encoding.GetByteCount((string)(object)type!) + sizeof(uint),
            ValueTags.Blob => ((byte[])(object)type!).Length,
            ValueTags.HashedBlob => ((byte[])(object)type!).Length + sizeof(ulong),
            ValueTags.Reference => sizeof(ulong),
            ValueTags.Tuple2 => throw new NotSupportedException(),
            ValueTags.Tuple3 => throw new NotSupportedException(),
            ValueTags.Tuple4 => throw new NotSupportedException(),
            ValueTags.Tuple5 => throw new NotSupportedException(),
            ValueTags.Tuple6 => throw new NotSupportedException(),
            ValueTags.Tuple7 => throw new NotSupportedException(),
            ValueTags.Tuple8 => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
        };
    }
    


    private static void WriteNull<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        // Do Nothing
    }

    private static void WriteAscii<TWriter>(string s, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var size = s.Length;
        var span = writer.GetSpan(size + sizeof(uint));
        MemoryMarshal.Write(span, (uint)size);
        AsciiEncoding.GetBytes(s, span.SliceFast(sizeof(uint)));
        writer.Advance(size + sizeof(uint));
    }

    private static void WriteUtf8<TWriter>(string s, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var size = Utf8Encoding.GetByteCount(s);
        var span = writer.GetSpan(size + sizeof(uint));
        MemoryMarshal.Write(span, (uint)size);
        Utf8Encoding.GetBytes(s, span.SliceFast(sizeof(uint)));
        writer.Advance(size + sizeof(uint));
    }
    
    
    public virtual TValueType ReadValue(ReadOnlySpan<byte> span, ValueTags tag, AttributeResolver resolver)
    {
        return LowLevelType switch
        {
            ValueTags.Null => NullFromLowLevel(),
            ValueTags.UInt8 => FromLowLevel(ReadUnmanaged<byte>(span, out _), tag, resolver),
            ValueTags.UInt16 => FromLowLevel(ReadUnmanaged<ushort>(span, out _), tag, registryId),
            ValueTags.UInt32 => FromLowLevel(ReadUnmanaged<uint>(span, out _), tag, registryId),
            ValueTags.UInt64 => FromLowLevel(ReadUnmanaged<ulong>(span, out _), tag, registryId),
            ValueTags.UInt128 => FromLowLevel(ReadUnmanaged<UInt128>(span, out _), tag, registryId),
            ValueTags.Int16 => FromLowLevel(ReadUnmanaged<short>(span, out _), tag, registryId),
            ValueTags.Int32 => FromLowLevel(ReadUnmanaged<int>(span, out _), tag, registryId),
            ValueTags.Int64 => FromLowLevel(ReadUnmanaged<long>(span, out _), tag, registryId),
            ValueTags.Int128 => FromLowLevel(ReadUnmanaged<Int128>(span, out _), tag, registryId),
            ValueTags.Float32 => FromLowLevel(ReadUnmanaged<float>(span, out _), tag, registryId),
            ValueTags.Float64 => FromLowLevel(ReadUnmanaged<double>(span, out _), tag, registryId),
            ValueTags.Reference => FromLowLevel(ReadUnmanaged<ulong>(span, out _), tag, registryId),
            ValueTags.Ascii => FromLowLevel(ReadAscii(span, out _), tag, registryId),
            ValueTags.Utf8 => FromLowLevel(ReadUtf8(span, out _), tag, registryId),
            ValueTags.Utf8Insensitive => FromLowLevel(ReadUtf8(span, out _), tag, registryId),
            ValueTags.Blob => FromLowLevel(span, tag, registryId),
            ValueTags.HashedBlob => FromLowLevel(span.SliceFast(sizeof(ulong)), tag, registryId),
            _ => throw new UnsupportedLowLevelReadType(tag)
        };
    }
    
    /// <summary>
    /// Reads a low-level value of a specific type from the given span
    /// </summary>
    protected TLowLevel ReadValue<TLowLevel>(ReadOnlySpan<byte> span, ValueTags tag, RegistryId registryId, out int size)
    {
        size = sizeof(ulong);
        return tag switch
        {
            ValueTags.Null => (TLowLevel)(object)NullFromLowLevel()!,
            ValueTags.UInt8 => (TLowLevel)(object)ReadUnmanaged<byte>(span, out size),
            ValueTags.UInt16 => (TLowLevel)(object)ReadUnmanaged<ushort>(span, out size),
            ValueTags.UInt32 => (TLowLevel)(object)ReadUnmanaged<uint>(span, out size),
            ValueTags.UInt64 => (TLowLevel)(object)ReadUnmanaged<ulong>(span, out size),
            ValueTags.UInt128 => (TLowLevel)(object)ReadUnmanaged<UInt128>(span, out size),
            ValueTags.Int16 => (TLowLevel)(object)ReadUnmanaged<short>(span, out size),
            ValueTags.Int32 => (TLowLevel)(object)ReadUnmanaged<int>(span, out size),
            ValueTags.Int64 => (TLowLevel)(object)ReadUnmanaged<long>(span, out size),
            ValueTags.Int128 => (TLowLevel)(object)ReadUnmanaged<Int128>(span, out size),
            ValueTags.Float32 => (TLowLevel)(object)ReadUnmanaged<float>(span, out size),
            ValueTags.Float64 => (TLowLevel)(object)ReadUnmanaged<double>(span, out size),
            ValueTags.Reference => (TLowLevel)(object)ReadUnmanaged<ulong>(span, out size),
            ValueTags.Ascii => (TLowLevel)(object)ReadAscii(span, out size),
            ValueTags.Utf8 => (TLowLevel)(object)ReadUtf8(span, out size),
            ValueTags.Utf8Insensitive => (TLowLevel)(object)ReadUtf8(span, out size),
            ValueTags.Blob => (TLowLevel)(object)FromLowLevel(span, tag, registryId)!,
            ValueTags.HashedBlob => (TLowLevel)(object)span.SliceFast(sizeof(ulong)).ToArray(),
            _ => throw new UnsupportedLowLevelReadType(tag),
        };
    }


    private TValueType NullFromLowLevel()
    {
        return default!;
    }

    private string ReadUtf8(ReadOnlySpan<byte> span, out int readSize)
    {
        var size = MemoryMarshal.Read<uint>(span);
        readSize = (int)size + sizeof(uint);
        return Utf8Encoding.GetString(span.Slice(sizeof(uint), (int)size));
    }

    private string ReadAscii(ReadOnlySpan<byte> span, out int readSize)
    {
        var size = MemoryMarshal.Read<uint>(span);
        readSize = (int)size + sizeof(uint);
        return AsciiEncoding.GetString(span.Slice(sizeof(uint), (int)size));
    }

    private static unsafe void WriteUnmanaged<TWriter, TValue>(TValue value, TWriter writer)
        where TWriter : IBufferWriter<byte>
        where TValue : unmanaged
    {
        var span = writer.GetSpan(sizeof(TValue));
        MemoryMarshal.Write(span, value);
        writer.Advance(sizeof(TValue));
    }

    private TValue ReadUnmanaged<TValue>(ReadOnlySpan<byte> span, out int size)
        where TValue : unmanaged
    {
        unsafe
        {
            size = sizeof(TValue);
            return MemoryMarshal.Read<TValue>(span);
        }
    }

    /// <summary>
    /// Write a datom for this attribute to the given writer
    /// </summary>
    public virtual void Write<TWriter>(EntityId entityId, AttributeCache cache, TValueType value, TxId txId, bool isRetract, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        Debug.Assert(LowLevelType != ValueTags.Blob, "Blobs should overwrite this method and throw when ToLowLevel is called");
        var prefix = new KeyPrefix(entityId, cache.GetAttributeId(Id), txId, isRetract, LowLevelType);
        var span = writer.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, prefix);
        writer.Advance(KeyPrefix.Size);
        WriteValue(value, writer);
    }

    /// <summary>
    /// Write the key prefix for this attribute to the given writer
    /// </summary>
    protected void WritePrefix<TWriter>(EntityId entityId, RegistryId registryId, TxId txId, bool isRetract, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var prefix = new KeyPrefix(entityId, GetDbId(registryId), txId, isRetract, LowLevelType);
        var span = writer.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, prefix);
        writer.Advance(KeyPrefix.Size);
    }
}
