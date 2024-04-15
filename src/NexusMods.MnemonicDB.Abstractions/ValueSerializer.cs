using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

public class AValueSerializer
{
    #region Constants
    private const int MaxStackAlloc = 128;
    private static readonly Encoding AsciiEncoding = Encoding.ASCII;
    private static readonly Encoding Utf8Encoding = Encoding.UTF8;
    #endregion




    /// <summary>
    ///     Performs a highly optimized, sort between two value pointers.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="alen"></param>
    /// <param name="b"></param>
    /// <param name="blen"></param>
    /// <returns></returns>
    public static unsafe int CompareUnsafe(byte* a, int alen, byte* b, int blen)
    {
        var typeA = a[0];
        var typeB = b[0];

        if (typeA != typeB)
            return typeA.CompareTo(typeB);

        var aVal = a + 1;
        var bVal = b + 1;

        return (ValueTags)typeA switch
        {
            ValueTags.UInt8 => Compare<byte>(aVal, bVal),
            ValueTags.UInt16 => Compare<ushort>(aVal, bVal),
            ValueTags.UInt32 => Compare<uint>(aVal, bVal),
            ValueTags.UInt64 => Compare<ulong>(aVal, bVal),
            ValueTags.UInt128 => Compare<UInt128>(aVal, bVal),
            ValueTags.Int16 => Compare<short>(aVal, bVal),
            ValueTags.Int32 => Compare<int>(aVal, bVal),
            ValueTags.Int64 => Compare<long>(aVal, bVal),
            ValueTags.Int128 => Compare<Int128>(aVal, bVal),
            ValueTags.Float32 => Compare<float>(aVal, bVal),
            ValueTags.Float64 => Compare<double>(aVal, bVal),
            ValueTags.Ascii => CompareBlob(aVal, bVal),
            ValueTags.Utf8 => CompareBlob(aVal, bVal),
            ValueTags.Utf8Insensitive => CompareUtf8Insensitive(aVal, bVal),
            ValueTags.Blob => CompareBlob(aVal, bVal),
            _ => alen - blen
        };
    }

    private static unsafe int CompareBlob(byte* aVal, byte* bVal)
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


    private static unsafe int Compare<T>(byte* aVal, byte* bVal)
    where T : unmanaged, IComparable<T>
    {
        return ((T*)aVal)[0].CompareTo(((T*)bVal)[0]);
    }

}
