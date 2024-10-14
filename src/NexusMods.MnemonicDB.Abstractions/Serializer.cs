using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

using Tuple3_Ref_UShort_Utf8I = (EntityId, ushort, string);
using Tuple2_UShort_Utf8I = (ushort, string);

/// <summary>
/// Functions for serializing and deserializing data
/// </summary>
public static class Serializer
{
    /// <summary>
    /// The size of the portion of a hashed blob datom that is stored in the key vs the value
    /// </summary>
    public const int HashedBlobPrefixSize = KeyPrefix.Size + sizeof(uint) + sizeof(ulong);
    
    /// <summary>
    /// The portion of the value span that is the key of a hashed blob
    /// </summary>
    public const int HashedBlobHeaderSize = sizeof(uint) + sizeof(ulong);
    
    #region Encoders
    private static readonly Encoding ASCII = Encoding.ASCII;
    private static readonly Encoding UTF8 = Encoding.UTF8;
    #endregion

    #region Read
    /// <summary>
    /// Reads the value from the given span. Will throw an exception if the tag does
    /// not match the value type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this ValueTag tag, ReadOnlySpan<byte> span)
    {
        return tag switch
        {
            ValueTag.Null => (T)(object)Null.Instance,
            ValueTag.UInt8 => (T)(object)span[0],
            ValueTag.UInt16 => (T)(object)MemoryMarshal.Read<ushort>(span),
            ValueTag.UInt32 => (T)(object)MemoryMarshal.Read<uint>(span),
            ValueTag.UInt64 => (T)(object)MemoryMarshal.Read<ulong>(span),
            ValueTag.UInt128 => (T)(object)MemoryMarshal.Read<UInt128>(span),
            ValueTag.Int16 => (T)(object)MemoryMarshal.Read<short>(span),
            ValueTag.Int32 => (T)(object)MemoryMarshal.Read<int>(span),
            ValueTag.Int64 => (T)(object)MemoryMarshal.Read<long>(span),
            ValueTag.Int128 => (T)(object)MemoryMarshal.Read<Int128>(span),
            ValueTag.Float32 => (T)(object)MemoryMarshal.Read<float>(span),
            ValueTag.Float64 => (T)(object)MemoryMarshal.Read<double>(span),
            ValueTag.Ascii => (T)(object)ReadAscii(span),
            ValueTag.Utf8 => (T)(object)ReadUtf8(span),
            ValueTag.Utf8Insensitive => (T)(object)ReadUtf8(span),
            ValueTag.Blob => (T)(object)ReadBlob(span),
            ValueTag.HashedBlob => (T)(object)ReadHashedBlob(span),
            ValueTag.Reference => (T)(object)MemoryMarshal.Read<EntityId>(span),
            ValueTag.Tuple3_Ref_UShort_Utf8I => (T)(object)ReadTuple3_Ref_UShort_Utf8I(span),
            ValueTag.Tuple2_UShort_Utf8I => (T)(object)ReadTuple2_UShort_Utf8I(span),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown tag")
        };
    }

    private static (ushort, string) ReadTuple2_UShort_Utf8I(ReadOnlySpan<byte> span)
    {
        var value1 = MemoryMarshal.Read<ushort>(span);
        var value2 = ReadUtf8(span.SliceFast(sizeof(ushort)));
        return (value1, value2);
    }

    private static (EntityId, ushort, string) ReadTuple3_Ref_UShort_Utf8I(ReadOnlySpan<byte> span)
    {
        unsafe
        {
            var entityId = MemoryMarshal.Read<EntityId>(span);
            var value1 = MemoryMarshal.Read<ushort>(span.SliceFast(sizeof(EntityId)));
            var value2 = ReadUtf8(span.SliceFast(sizeof(EntityId) + sizeof(ushort)));
            return (entityId, value1, value2);
        }
    }

    private static Memory<byte> ReadBlob(ReadOnlySpan<byte> span)
    {
        var length = MemoryMarshal.Read<uint>(span);
        return span.SliceFast(sizeof(uint), (int)length).ToArray();
    }
    
    private static Memory<byte> ReadHashedBlob(ReadOnlySpan<byte> span)
    {
        const int hashSize = sizeof(ulong);
        var length = MemoryMarshal.Read<uint>(span);
        return span.SliceFast(sizeof(uint) + hashSize, (int)length).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadAscii(ReadOnlySpan<byte> span)
    {
        var length = MemoryMarshal.Read<uint>(span);
        return ASCII.GetString(span.SliceFast(sizeof(uint), (int)length));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadUtf8(ReadOnlySpan<byte> span)
    {
        var length = MemoryMarshal.Read<uint>(span);
        return UTF8.GetString(span.SliceFast(sizeof(uint), (int)length));
    }
    #endregion

    #region Write
    /// <summary>
    /// Writes the value to the writer. Will throw an exception if the tag does not match the value type
    /// </summary>
    public static void Write<TWriter, TValue>(this ValueTag tag, TValue value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        switch (value)
        {
            case Null:
                break;
            case byte v:
                WriteUnmanaged(v, writer);
                break;
            case ushort v:
                WriteUnmanaged(v, writer);
                break;
            case uint v:
                WriteUnmanaged(v, writer);
                break;
            case ulong v:
                WriteUnmanaged(v, writer);
                break;
            case UInt128 v:
                WriteUnmanaged(v, writer);
                break;
            case short v:
                WriteUnmanaged(v, writer);
                break;
            case int v:
                WriteUnmanaged(v, writer);
                break;
            case long v:
                WriteUnmanaged(v, writer);
                break;
            case Int128 v:
                WriteUnmanaged(v, writer);
                break;
            case float v:
                WriteUnmanaged(v, writer);
                break;
            case double v:
                WriteUnmanaged(v, writer);
                break;
            case string v when tag is ValueTag.Ascii:
                WriteAscii(v, writer);
                break;
            case string v when tag is ValueTag.Utf8 or ValueTag.Utf8Insensitive:
                WriteUtf8(v, writer);
                break;
            case Memory<byte> v when tag == ValueTag.Blob:
                WriteBlob(v, writer);
                break;
            case Memory<byte> v when tag == ValueTag.HashedBlob:
                WriteHashedBlob(v, writer);
                break;
            case EntityId v when tag == ValueTag.Reference:
                WriteUnmanaged(v, writer);
                break;
            case Tuple3_Ref_UShort_Utf8I v:
                WriteTuple3_Ref_UShort_Utf8I(v, writer);
                break;
            case Tuple2_UShort_Utf8I v:
                WriteTuple2_UShort_Utf8I(v, writer);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown value type of type " + value!.GetType());
        }
    }

    private static void WriteAscii<TWriter>(string value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var length = (uint)value.Length;
        var span = writer.GetSpan(sizeof(uint) + value.Length);
        MemoryMarshal.Write(span, length);
        ASCII.GetBytes(value, span.SliceFast(sizeof(uint)));
        writer.Advance(sizeof(uint) + value.Length);
    }

    private static void WriteUtf8<TWriter>(string value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var length = (uint)value.Length;
        var span = writer.GetSpan(sizeof(uint) + value.Length);
        MemoryMarshal.Write(span, length);
        UTF8.GetBytes(value, span.SliceFast(sizeof(uint)));
        writer.Advance(sizeof(uint) + value.Length);
    }

    private static void WriteBlob<TWriter>(Memory<byte> value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var length = (uint)value.Length;
        var span = writer.GetSpan(sizeof(uint) + value.Length);
        MemoryMarshal.Write(span, length);
        value.Span.CopyTo(span.SliceFast(sizeof(uint)));
        writer.Advance(sizeof(uint) + value.Length);
    }

    private static void WriteHashedBlob<TWriter>(Memory<byte> value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var length = (uint)value.Length;
        var hash = XxHash3.HashToUInt64(value.Span);
        var fullSize = sizeof(uint) + sizeof(ulong) + value.Length;
        var span = writer.GetSpan(fullSize);
        MemoryMarshal.Write(span, length);
        MemoryMarshal.Write(span.SliceFast(sizeof(uint)), hash);
        value.Span.CopyTo(span.SliceFast(sizeof(uint) + sizeof(ulong)));
        writer.Advance(fullSize);
    }

    private static void WriteTuple3_Ref_UShort_Utf8I<TWriter>((EntityId, ushort, string) value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        WriteUnmanaged(value.Item1, writer);
        WriteUnmanaged(value.Item2, writer);
        WriteUtf8(value.Item3, writer);
    }

    private static void WriteTuple2_UShort_Utf8I<TWriter>((ushort, string) value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        WriteUnmanaged(value.Item1, writer);
        WriteUtf8(value.Item2, writer);
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteUnmanaged<T, TWriter>(T value, TWriter writer)
        where TWriter : IBufferWriter<byte> where T : unmanaged
    {
        var span = writer.GetSpan(Marshal.SizeOf<T>());
        MemoryMarshal.Write(span, value);
        writer.Advance(Marshal.SizeOf<T>());
    }
    #endregion

    #region Comparion
    
    /// <summary>
    /// Compares two values with the given tags and pointers, the tags need to be the same
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Compare(ValueTag aTag, byte* aVal, int aLen, ValueTag bTag, byte* bVal, int bLen)
    {
        if (aTag != bTag)
            return aTag.CompareTo(bTag);
        
        return aTag.Compare(aVal, aLen, bVal, bLen);
    }

    /// <summary>
    /// Compares two datoms with the given prefixes and value pointers
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe int CompareDatoms(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        var typeA = aPrefix->ValueTag;
        var typeB = bPrefix->ValueTag;
        
        if (typeA != typeB)
            return typeA.CompareTo(typeB);

        return typeA.Compare(aPtr, aLen, bPtr, bLen);
    }

    /// <summary>
    /// Compare two values of the given tag
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Compare(this ValueTag tag, byte* aVal, int aLen, byte* bVal, int bLen)
    {
        if (aLen == 0 && bLen == 0)
            return aLen.CompareTo(bLen);
        
        return tag switch
        {
            ValueTag.Null => 0,
            ValueTag.UInt8 => (*aVal).CompareTo(*bVal),
            ValueTag.UInt16 => (*(ushort*)aVal).CompareTo(*(ushort*)bVal),
            ValueTag.UInt32 => (*(uint*)aVal).CompareTo(*(uint*)bVal),
            ValueTag.UInt64 => (*(ulong*)aVal).CompareTo(*(ulong*)bVal),
            ValueTag.UInt128 => (*(UInt128*)aVal).CompareTo(*(UInt128*)bVal),
            ValueTag.Int16 => (*(short*)aVal).CompareTo(*(short*)bVal),
            ValueTag.Int32 => (*(int*)aVal).CompareTo(*(int*)bVal),
            ValueTag.Int64 => (*(long*)aVal).CompareTo(*(long*)bVal),
            ValueTag.Int128 => (*(Int128*)aVal).CompareTo(*(Int128*)bVal),
            ValueTag.Float32 => (*(float*)aVal).CompareTo(*(float*)bVal),
            ValueTag.Float64 => (*(double*)aVal).CompareTo(*(double*)bVal),
            ValueTag.Ascii => CompareAscii(aVal, aLen, bVal, bLen),
            ValueTag.Utf8 => CompareUtf8(aVal, aLen, bVal, bLen),
            ValueTag.Utf8Insensitive => CompareUtf8(aVal, aLen, bVal, bLen),
            ValueTag.Blob => CompareBlob(aVal, aLen, bVal, bLen),
            ValueTag.HashedBlob => CompareHashedBlob(aVal, aLen, bVal, bLen),
            ValueTag.Reference => (*(EntityId*)aVal).CompareTo(*(EntityId*)bVal),
            ValueTag.Tuple3_Ref_UShort_Utf8I => CompareTuple3_Ref_UShort_Utf8I(aVal, aLen, bVal, bLen),
            ValueTag.Tuple2_UShort_Utf8I => CompareTuple2_UShort_Utf8I(aVal, aLen, bVal, bLen),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown tag")
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int CompareAscii(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        return new Span<byte>(aVal + sizeof(uint), aLen - sizeof(uint))
            .SequenceCompareTo(new Span<byte>(bVal + sizeof(uint), bLen - sizeof(uint)));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int CompareUtf8(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        return new Span<byte>(aVal + sizeof(uint), aLen - sizeof(uint))
            .SequenceCompareTo(new Span<byte>(bVal + sizeof(uint), bLen - sizeof(uint)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int CompareBlob(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var aSpan = new ReadOnlySpan<byte>(aVal, aLen);
        var bSpan = new ReadOnlySpan<byte>(bVal, bLen);
        return aSpan.SequenceCompareTo(bSpan);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int CompareHashedBlob(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var hash1 = *(ulong*)(aVal + sizeof(uint));
        var hash2 = *(ulong*)(bVal + sizeof(uint));
        return hash1.CompareTo(hash2);
    }

    private static unsafe int CompareTuple3_Ref_UShort_Utf8I(byte* aVal, int aLen, byte* bVal, int bLen)
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

    private static unsafe int CompareTuple2_UShort_Utf8I(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var aUShort = *(ushort*)aVal;
        var bUShort = *(ushort*)bVal;
        var ushortComparison = aUShort.CompareTo(bUShort);
        if (ushortComparison != 0) return ushortComparison;

        var aStr = Encoding.UTF8.GetString(aVal + sizeof(ushort), aLen - sizeof(ushort));
        var bStr = Encoding.UTF8.GetString(bVal + sizeof(ushort), bLen - sizeof(ushort));
        return string.Compare(aStr, bStr, StringComparison.Ordinal);
    }
    #endregion
    
    #region Remap
    
    /// <summary>
    /// Use the given function to remap any references found in the span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Remap(this ValueTag tag, Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
        switch (tag)
        {
            case ValueTag.Reference:
            case ValueTag.Tuple3_Ref_UShort_Utf8I:
                MemoryMarshal.Write(span, remapFn(MemoryMarshal.Read<EntityId>(span)));
                break;
            default:
                return;
        }
    }
    #endregion

    #region ValueConversion
    
    public static void ConvertValue<TWriter>(this ValueTag srcTag, ReadOnlySpan<byte> srcSpan, ValueTag destTag, TWriter destWriter)
        where TWriter : IBufferWriter<byte>
    {

        switch (srcTag, destTag)
        {
            case (ValueTag.UInt8, ValueTag.UInt16):
                WriteUnmanaged((ushort)MemoryMarshal.Read<byte>(srcSpan), destWriter);
                break;
            
            default:
                throw new NotSupportedException("Conversion not supported from " + srcTag + " to " + destTag);
        }
    }

    #endregion

}

