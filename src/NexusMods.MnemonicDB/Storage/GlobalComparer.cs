using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;

namespace NexusMods.MnemonicDB.Storage;

/// <summary>
/// Comparer that also includes the index type
/// </summary>
public class GlobalComparer : IComparer<byte[]>
{
    /// <summary>
    /// Compare two byte arrays that are prefixed by the index type
    /// </summary>
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var aIndex = aPtr[0];
        var bIndex = bPtr[0];

        var cmp = aIndex.CompareTo(bIndex);
        if (cmp != 0)
            return cmp;

        var aPtrNext = aPtr + 1;
        var aLenNext = aLen - 1;
        var bPtrNext = bPtr + 1;
        var bLenNext = bLen - 1;

        switch ((IndexType)aIndex)
        {
            case IndexType.TxLog:
                return TxLogComparator.Compare(aPtrNext, aLenNext, bPtrNext, bLenNext);
            case IndexType.EAVTCurrent:
            case IndexType.EAVTHistory:
                return EAVTComparator.Compare(aPtrNext, aLenNext, bPtrNext, bLenNext);
            case IndexType.AEVTCurrent:
            case IndexType.AEVTHistory:
                return AEVTComparator.Compare(aPtrNext, aLenNext, bPtrNext, bLenNext);
            case IndexType.AVETCurrent:
            case IndexType.AVETHistory:
                return AVETComparator.Compare(aPtrNext, aLenNext, bPtrNext, bLenNext);
            case IndexType.VAETCurrent:
            case IndexType.VAETHistory:
                return VAETComparator.Compare(aPtrNext, aLenNext, bPtrNext, bLenNext);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc />
    public unsafe int Compare(byte[]? x, byte[]? y)
    {
        if (x == null && y == null)
            return 0;
        if (x == null)
            return -1;
        if (y == null)
            return 1;

        fixed (byte* xPtr = x)
        fixed (byte* yPtr = y)
        {
            return Compare(xPtr, x.Length, yPtr, y.Length);
        }
    }
}
