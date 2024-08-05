using System;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
///     Compares values and assumes that some previous comparator will guarantee that the values are of the same attribute.
/// </summary>
public class ValueComparer : IElementComparer
{
    #region Constants
    private static readonly Encoding Utf8Encoding = Encoding.UTF8;
    #endregion

    /// <inheritdoc />
    public static unsafe int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        var prefixA = *(KeyPrefix*)aPtr;
        var prefixB = *(KeyPrefix*)bPtr;

        var typeA = prefixA.ValueTag;
        var typeB = prefixB.ValueTag;

        return CompareValues(typeA, aPtr, aLen, typeB, bPtr, bLen);
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var typeA = ((KeyPrefix*)aPtr)->ValueTag;
        var typeB = ((KeyPrefix*)bPtr)->ValueTag;

        return CompareValues(typeA, aPtr + KeyPrefix.Size, aLen - KeyPrefix.Size, typeB, bPtr + KeyPrefix.Size, bLen - KeyPrefix.Size);
    }

    /// <inheritdoc />
    public static int Compare(in Datom a, in Datom b)
    {
        var typeA = a.Prefix.ValueTag;
        var typeB = b.Prefix.ValueTag;

        unsafe
        {
            fixed (byte* aPtr = a.ValueSpan)
            {
                fixed (byte* bPtr = b.ValueSpan)
                {
                    return CompareValues(typeA, aPtr, a.ValueSpan.Length, typeB, bPtr, b.ValueSpan.Length);
                }
            }
        }
    }

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var typeA = KeyPrefix.Read(a).ValueTag;
        var typeB = KeyPrefix.Read(b).ValueTag;

        unsafe
        {
            fixed (byte* aPtr = a.SliceFast(KeyPrefix.Size))
            {
                fixed (byte* bPtr = b.SliceFast(KeyPrefix.Size))
                {
                    return CompareValues(typeA, aPtr + KeyPrefix.Size, a.Length - KeyPrefix.Size, typeB, bPtr + KeyPrefix.Size, b.Length - KeyPrefix.Size);
                }
            }
        }
    }

    /// <summary>
    ///     Performs a highly optimized, sort between two value pointers.
    /// </summary>
    public static unsafe int CompareValues(ValueTags typeA, byte* aVal, int aLen, ValueTags typeB, byte* bVal, int bLen)
    {
        if (aLen == 0 || bLen == 0)
            return aLen.CompareTo(bLen);
        if (typeA != typeB)
            return typeA.CompareTo(typeB);

        return typeA switch
        {
            ValueTags.Null => 0,
            ValueTags.UInt8 => CompareInternal<byte>(aVal, bVal),
            ValueTags.UInt16 => CompareInternal<ushort>(aVal, bVal),
            ValueTags.UInt32 => CompareInternal<uint>(aVal, bVal),
            ValueTags.UInt64 => CompareInternal<ulong>(aVal, bVal),
            ValueTags.UInt128 => CompareInternal<UInt128>(aVal, bVal),
            ValueTags.Int16 => CompareInternal<short>(aVal, bVal),
            ValueTags.Int32 => CompareInternal<int>(aVal, bVal),
            ValueTags.Int64 => CompareInternal<long>(aVal, bVal),
            ValueTags.Int128 => CompareInternal<Int128>(aVal, bVal),
            ValueTags.Float32 => CompareInternal<float>(aVal, bVal),
            ValueTags.Float64 => CompareInternal<double>(aVal, bVal),
            ValueTags.Ascii => CompareAscii(aVal, aLen, bVal, bLen),
            ValueTags.Utf8 => CompareUtf8(aVal, aLen, bVal, bLen),
            ValueTags.Utf8Insensitive => CompareUtf8Insensitive(aVal, aLen, bVal, bLen),
            ValueTags.Blob => CompareBlobInternal(aVal, aLen, bVal, bLen),
            // HashedBlob is a special case, we compare the hashes not the blobs
            ValueTags.HashedBlob => CompareInternal<ulong>(aVal, bVal),
            ValueTags.Reference => CompareInternal<ulong>(aVal, bVal),
            ValueTags.Tuple2 => CompareTuples2(aVal, aLen, bVal, bLen),
            ValueTags.Tuple3 => CompareTuples3(aVal, aLen, bVal, bLen),
            _ => ThrowInvalidCompare(typeA)
        };
    }

    private static unsafe int CompareTuples2(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var typeA1 = (ValueTags)aVal[0];
        var typeB1 = (ValueTags)bVal[0];
        
        if (typeA1 != typeB1)
            return typeA1.CompareTo(typeB1);
        
        var aLen1 = GetValueLength(typeA1, aVal);
        var bLen1 = GetValueLength(typeB1, bVal);
        
        // +2 for the type tags (A, B)
        var cmp = CompareValues(typeA1, aVal + 2, aLen1, typeB1, bVal + 2, bLen1);
        if (cmp != 0)
            return cmp;
        
        var typeA2 = (ValueTags)aVal[1];
        var typeB2 = (ValueTags)bVal[1];
        
        if (typeA2 != typeB2)
            return typeA2.CompareTo(typeB2);
        
        // +2 for the type tags (A, B), + the length of the first value
        var aLen2 = GetValueLength(typeA2, aVal + 2 + aLen1);
        var bLen2 = GetValueLength(typeB2, bVal + 2 + bLen1);
        
        return CompareValues(typeA2, aVal + 2 + aLen1 + 2, aLen2, typeB2, bVal + 2 + bLen1 + 2, bLen2);
    }
    
    private static unsafe int CompareTuples3(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var typeA1 = (ValueTags)aVal[0];
        var typeB1 = (ValueTags)bVal[0];
        
        if (typeA1 != typeB1)
            return typeA1.CompareTo(typeB1);
        
        var aLen1 = GetValueLength(typeA1, aVal);
        var bLen1 = GetValueLength(typeB1, bVal);
        
        // +3 for the type tags (A, B, C)
        var cmp = CompareValues(typeA1, aVal + 3, aLen1, typeB1, bVal + 3, bLen1);
        if (cmp != 0)
            return cmp;
        
        var typeA2 = (ValueTags)aVal[1];
        var typeB2 = (ValueTags)bVal[1];
        
        if (typeA2 != typeB2)
            return typeA2.CompareTo(typeB2);
        
        // +2 for the type tags (A, B), + the length of the first value
        var aLen2 = GetValueLength(typeA2, aVal + 3 + aLen1);
        var bLen2 = GetValueLength(typeB2, bVal + 3 + bLen1);
        
        cmp = CompareValues(typeA2, aVal + 3 + aLen1 + 3, aLen2, typeB2, bVal + 3 + bLen1 + 3, bLen2);
        if (cmp != 0)
            return cmp;
        
        var typeA3 = (ValueTags)aVal[2];
        var typeB3 = (ValueTags)bVal[2];
        
        if (typeA3 != typeB3)
            return typeA3.CompareTo(typeB3);
        
        var aLen3 = GetValueLength(typeA3, aVal + 3 + aLen1 + aLen2);
        var bLen3 = GetValueLength(typeB3, bVal + 3 + bLen1 + bLen2);
        
        return CompareValues(typeA3, aVal + 3 + aLen1 + aLen2, aLen3, typeB3, bVal + 3 + bLen1 + bLen2, bLen3);
    }
    

    private static unsafe int GetValueLength(ValueTags tag, byte* aVal)
    {
        switch (tag)
        {
            case ValueTags.Null:
                return 0;
            case ValueTags.UInt8:
                return sizeof(byte);
            case ValueTags.UInt16:
                return sizeof(ushort);
            case ValueTags.UInt32:
                return sizeof(uint);
            case ValueTags.UInt64:
                return sizeof(ulong);
            case ValueTags.UInt128:
                return sizeof(UInt128);
            case ValueTags.Int16:
                return sizeof(short);
            case ValueTags.Int32:
                return sizeof(int);
            case ValueTags.Int64:
                return sizeof(long);
            case ValueTags.Int128:
                return sizeof(Int128);
            case ValueTags.Float32:
                return sizeof(float);
            case ValueTags.Float64:
                return sizeof(double);
            case ValueTags.Ascii:
                return ((int*)aVal)[0] + sizeof(int);
            case ValueTags.Utf8:
                return ((int*)aVal)[0] + sizeof(int);
            case ValueTags.Utf8Insensitive:
                return ((int*)aVal)[0] + sizeof(int);
            case ValueTags.Blob:
                return ((int*)aVal)[0] + sizeof(int);
            case ValueTags.HashedBlob:
                return sizeof(ulong);
            case ValueTags.Reference:
                return sizeof(ulong);
            case ValueTags.Tuple2:
            case ValueTags.Tuple3:
            case ValueTags.Tuple4:
            case ValueTags.Tuple5:
            case ValueTags.Tuple6:
            case ValueTags.Tuple7:
            case ValueTags.Tuple8:
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
        }
    }

    private static int ThrowInvalidCompare(ValueTags typeA)
    {
        throw new InvalidOperationException($"Invalid comparison for type {typeA}"); 
    }

    private static unsafe int CompareBlobInternal(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        return new Span<byte>(aVal, aLen)
            .SequenceCompareTo(new Span<byte>(bVal, bLen));
    }
    
    private static unsafe int CompareAscii(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        return new Span<byte>(aVal + sizeof(uint), aLen - sizeof(uint))
            .SequenceCompareTo(new Span<byte>(bVal + sizeof(uint), bLen - sizeof(uint)));
    }
    
    private static unsafe int CompareUtf8(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        return new Span<byte>(aVal + sizeof(uint), aLen - sizeof(uint))
            .SequenceCompareTo(new Span<byte>(bVal + sizeof(uint), bLen - sizeof(uint)));
    }

    private static unsafe int CompareUtf8Insensitive(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var strA = Utf8Encoding.GetString(aVal + sizeof(uint), aLen - sizeof(uint));
        var strB = Utf8Encoding.GetString(bVal + sizeof(uint), bLen - sizeof(uint));

        return string.Compare(strA, strB, StringComparison.InvariantCultureIgnoreCase);
    }

    private static unsafe int CompareInternal<T>(byte* aVal, byte* bVal)
    where T : unmanaged, IComparable<T>
    {
        return ((T*)aVal)[0].CompareTo(((T*)bVal)[0]);
    }

}
