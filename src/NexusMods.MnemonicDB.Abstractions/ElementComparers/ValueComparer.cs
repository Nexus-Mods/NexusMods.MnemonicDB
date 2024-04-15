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

        return CompareValues(ptrA, aSize, ptrB, bSize);
    }

    /// <summary>
    ///     Performs a highly optimized, sort between two value pointers.
    /// </summary>
    public static unsafe int CompareValues(byte* a, int alen, byte* b, int blen)
    {
        var typeA = a[0];
        var typeB = b[0];

        if (typeA != typeB)
            return typeA.CompareTo(typeB);

        var aVal = a + 1;
        var bVal = b + 1;

        return (ValueTags)typeA switch
        {
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
            ValueTags.Ascii => CompareBlobInternal(aVal, bVal),
            ValueTags.Utf8 => CompareBlobInternal(aVal, bVal),
            ValueTags.Utf8Insensitive => CompareUtf8Insensitive(aVal, bVal),
            ValueTags.Blob => CompareBlobInternal(aVal, bVal),
            _ => alen - blen
        };
    }

    private static unsafe int CompareBlobInternal(byte* aVal, byte* bVal)
    {
        var aSize = GetInlineSize(ref aVal);
        var bSize = GetInlineSize(ref bVal);

        return new Span<byte>(aVal, aSize)
            .SequenceCompareTo(new Span<byte>(bVal, bSize));
    }

    private static unsafe int CompareUtf8Insensitive(byte* aVal, byte* bVal)
    {
        var aSize = GetInlineSize(ref aVal);
        var bSize = GetInlineSize(ref bVal);

        var strA = Utf8Encoding.GetString(aVal, aSize);
        var strB = Utf8Encoding.GetString(bVal, bSize);

        return string.Compare(strA, strB, StringComparison.InvariantCultureIgnoreCase);
    }

    private static unsafe int GetInlineSize(ref byte* aVal)
    {
        int aSize = aVal[0];
        if (aSize == byte.MaxValue)
        {
            aVal += 1;
            aSize = ((ushort*)aVal)[0];
        }

        return aSize;
    }


    private static unsafe int CompareInternal<T>(byte* aVal, byte* bVal)
    where T : unmanaged, IComparable<T>
    {
        return ((T*)aVal)[0].CompareTo(((T*)bVal)[0]);
    }



}
