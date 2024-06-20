using System;
using System.Text;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
///     Compares values and assumes that some previous comparator will guarantee that the values are of the same attribute.
/// </summary>
public class ValueComparer : IElementComparer
{
    #region Constants
    private const int MaxStackAlloc = 128;
    private static readonly Encoding AsciiEncoding = Encoding.ASCII;
    private static readonly Encoding Utf8Encoding = Encoding.UTF8;
    #endregion


    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var ptrA = aPtr + sizeof(KeyPrefix);
        var ptrB = bPtr + sizeof(KeyPrefix);
        var aSize = aLen - sizeof(KeyPrefix);
        var bSize = bLen - sizeof(KeyPrefix);

        var prefixA = *(KeyPrefix*)aPtr;
        var prefixB = *(KeyPrefix*)bPtr;

        var typeA = prefixA.ValueTag;
        var typeB = prefixB.ValueTag;

        return CompareValues(typeA, ptrA, aSize, typeB, ptrB, bSize);
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
            ValueTags.Ascii => CompareBlobInternal(aVal, aLen, bVal, bLen),
            ValueTags.Utf8 => CompareBlobInternal(aVal, aLen, bVal, bLen),
            ValueTags.Utf8Insensitive => CompareUtf8Insensitive(aVal, aLen, bVal, bLen),
            ValueTags.Blob => CompareBlobInternal(aVal, aLen, bVal, bLen),
            // HashedBlob is a special case, we compare the hashes not the blobs
            ValueTags.HashedBlob => CompareInternal<ulong>(aVal, bVal),
            ValueTags.Reference => CompareInternal<ulong>(aVal, bVal),
            _ => ThrowInvalidCompare()
        };
    }

    private static int ThrowInvalidCompare()
    {
        throw new InvalidOperationException("Invalid compare type");
    }

    private static unsafe int CompareBlobInternal(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        return new Span<byte>(aVal, aLen)
            .SequenceCompareTo(new Span<byte>(bVal, bLen));
    }

    private static unsafe int CompareUtf8Insensitive(byte* aVal, int aLen, byte* bVal, int bLen)
    {
        var strA = Utf8Encoding.GetString(aVal, aLen);
        var strB = Utf8Encoding.GetString(bVal, bLen);

        return string.Compare(strA, strB, StringComparison.InvariantCultureIgnoreCase);
    }

    private static unsafe int CompareInternal<T>(byte* aVal, byte* bVal)
    where T : unmanaged, IComparable<T>
    {
        return ((T*)aVal)[0].CompareTo(((T*)bVal)[0]);
    }
}
