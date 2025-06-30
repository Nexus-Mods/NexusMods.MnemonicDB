using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

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
    public const int HashedBlobPrefixSize = KeyPrefix.Size + sizeof(ulong);
    
    /// <summary>
    /// The portion of the value span that is the key of a hashed blob
    /// </summary>
    public const int HashedBlobHeaderSize = sizeof(ulong);

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
            ValueTag.UInt8 => (T)(object)UInt8Serializer.Read(span),
            ValueTag.UInt16 => (T)(object)UInt16Serializer.Read(span),
            ValueTag.UInt32 => (T)(object)UInt32Serializer.Read(span),
            ValueTag.UInt64 => (T)(object)UInt64Serializer.Read(span),
            ValueTag.UInt128 => (T)(object)UInt128Serializer.Read(span),
            ValueTag.Int16 => (T)(object)Int16Serializer.Read(span),
            ValueTag.Int32 => (T)(object)Int32Serializer.Read(span),
            ValueTag.Int64 => (T)(object)Int64Serializer.Read(span),
            ValueTag.Int128 => (T)(object)Int128Serializer.Read(span),
            ValueTag.Float32 => (T)(object)Float32Serializer.Read(span),
            ValueTag.Float64 => (T)(object)Float64Serializer.Read(span),
            ValueTag.Ascii => (T)(object)AsciiSerializer.Read(span),
            ValueTag.Utf8 => (T)(object)Utf8Serializer.Read(span),
            ValueTag.Utf8Insensitive => (T)(object)Utf8InsensitiveSerializer.Read(span),
            ValueTag.Blob => (T)(object)BlobSerializer.Read(span),
            ValueTag.HashedBlob => (T)(object)HashedBlobSerializer.Read(span),
            ValueTag.Reference => (T)(object)EntityIdSerializer.Read(span),
            ValueTag.Tuple3_Ref_UShort_Utf8I => (T)(object)Tuple3_Ref_UShort_Utf8I_Serializer.Read(span),
            ValueTag.Tuple2_UShort_Utf8I => (T)(object)Tuple2_UShort_Utf8I_Serializer.Read(span),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown tag")
        };
    }
    
    /// <summary>
    /// Gets the low-level C# type that corresponds to the given tag.
    /// </summary>
    public static Type LowLevelType(this ValueTag tag)
    {
        return tag switch
        {
            ValueTag.Null => typeof(Null),
            ValueTag.UInt8 => typeof(byte),
            ValueTag.UInt16 => typeof(ushort),
            ValueTag.UInt32 => typeof(uint),
            ValueTag.UInt64 => typeof(ulong),
            ValueTag.UInt128 => typeof(UInt128),
            ValueTag.Int16 => typeof(short),
            ValueTag.Int32 => typeof(int),
            ValueTag.Int64 => typeof(long),
            ValueTag.Int128 => typeof(Int128),
            ValueTag.Float32 => typeof(float),
            ValueTag.Float64 => typeof(double),
            ValueTag.Ascii or ValueTag.Utf8 or ValueTag.Utf8Insensitive => typeof(string),
            ValueTag.Blob or ValueTag.HashedBlob => typeof(Memory<byte>),
            ValueTag.Reference => typeof(EntityId),
            ValueTag.Tuple3_Ref_UShort_Utf8I => typeof(Tuple3_Ref_UShort_Utf8I),
            ValueTag.Tuple2_UShort_Utf8I => typeof(Tuple2_UShort_Utf8I),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown tag")
        };
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
                UInt8Serializer.Write(v, writer);
                break;
            case ushort v:
                UInt16Serializer.Write(v, writer);
                break;
            case uint v:
                UInt32Serializer.Write(v, writer);
                break;
            case ulong v: 
                UInt64Serializer.Write(v, writer);
                break;
            case UInt128 v:
                UInt128Serializer.Write(v, writer);
                break;
            case short v:
                Int16Serializer.Write(v, writer);
                break;
            case int v:
                Int32Serializer.Write(v, writer);
                break;
            case long v:
                Int64Serializer.Write(v, writer);
                break;
            case Int128 v:
                Int128Serializer.Write(v, writer);
                break;
            case float v:
                Float32Serializer.Write(v, writer);
                break;
            case double v:
                Float64Serializer.Write(v, writer);
                break;
            case string v when tag is ValueTag.Ascii:
                AsciiSerializer.Write(v, writer);
                break;
            case string v when tag is ValueTag.Utf8:
                Utf8Serializer.Write(v, writer);
                break;
            case string v when tag is ValueTag.Utf8Insensitive:
                Utf8InsensitiveSerializer.Write(v, writer);
                break;
            case Memory<byte> v when tag == ValueTag.Blob:
                BlobSerializer.Write(v, writer);
                break;
            case Memory<byte> v when tag == ValueTag.HashedBlob:
                HashedBlobSerializer.Write(v, writer);
                break;
            case EntityId v when tag == ValueTag.Reference:
                EntityIdSerializer.Write(v, writer);
                break;
            case Tuple3_Ref_UShort_Utf8I v:
                Tuple3_Ref_UShort_Utf8I_Serializer.Write(v, writer);
                break;
            case Tuple2_UShort_Utf8I v:
                Tuple2_UShort_Utf8I_Serializer.Write(v, writer);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown value type of type " + value!.GetType());
        }
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
            ValueTag.Null => NullSerializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.UInt8 => UInt8Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.UInt16 => UInt16Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.UInt32 => UInt32Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.UInt64 => UInt64Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.UInt128 => UInt128Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Int16 => Int16Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Int32 => Int32Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Int64 => Int64Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Int128 => Int128Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Float32 => Float32Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Float64 => Float64Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Ascii => AsciiSerializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Utf8 => Utf8Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Utf8Insensitive => Utf8InsensitiveSerializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Blob => BlobSerializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.HashedBlob => HashedBlobSerializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Reference => EntityIdSerializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Tuple3_Ref_UShort_Utf8I => Tuple3_Ref_UShort_Utf8I_Serializer.Compare(aVal, aLen, bVal, bLen),
            ValueTag.Tuple2_UShort_Utf8I => Tuple2_UShort_Utf8I_Serializer.Compare(aVal, aLen, bVal, bLen),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown tag")
        };
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
                EntityIdSerializer.Remap(span, remapFn);
                break;
            case ValueTag.Tuple3_Ref_UShort_Utf8I:
                Tuple3_Ref_UShort_Utf8I_Serializer.Remap(span, remapFn);
                break;
            default:
                return;
        }
    }
    #endregion

    #region ValueConversion
    
    /// <summary>
    /// Convert the value from the source tag to the destination tag if possible, throws an exception if the conversion is not supported
    /// </summary>
    public static void ConvertValue<TWriter>(this ValueTag srcTag, ReadOnlySpan<byte> srcSpan, ValueTag destTag, TWriter destWriter)
        where TWriter : IBufferWriter<byte>
    {

        try
        {
            switch (srcTag, destTag)
            {
                case (ValueTag.UInt8, ValueTag.UInt16):
                    UInt16Serializer.Write(UInt8Serializer.Read(srcSpan), destWriter);
                    break;
                case (ValueTag.Utf8, ValueTag.UInt64):
                    UInt64Serializer.Write(ulong.Parse(Utf8Serializer.Read(srcSpan)), destWriter);
                    break;
                default:
                    throw new NotSupportedException("Conversion not supported from " + srcTag + " to " + destTag);
            }
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to convert ({srcTag.Read<object>(srcSpan)}) value from " + srcTag + " to " + destTag, e);
        }
    }

    #endregion

}

