using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Comparer that also includes the index type
/// </summary>
public sealed class GlobalComparer : IComparer<byte[]>
{
    /// <summary>
    /// Compare two byte arrays that are prefixed by the index type
    /// </summary>
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var prefixA = (KeyPrefix*)aPtr;
        var prefixB = (KeyPrefix*)bPtr;
        
        var cmp = ((byte)prefixA->Index).CompareTo((byte)prefixB->Index);
        if (cmp != 0)
            return cmp;

        return prefixA->Index switch
        {
            IndexType.TxLog => TxLogComparator.Compare(aPtr, aLen, bPtr, bLen),
            IndexType.EAVTCurrent or IndexType.EAVTHistory => EAVTComparator.Compare(aPtr, aLen, bPtr, bLen),
            IndexType.AEVTCurrent or IndexType.AEVTHistory => AEVTComparator.Compare(aPtr, aLen, bPtr, bLen),
            IndexType.AVETCurrent or IndexType.AVETHistory => AVETComparator.Compare(aPtr, aLen, bPtr, bLen),
            IndexType.VAETCurrent or IndexType.VAETHistory => VAETComparator.Compare(aPtr, aLen, bPtr, bLen),
            _ => ThrowArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Compare two datoms represented as spans
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        fixed (byte* aPtr = a)
        {
            fixed(byte* bPtr = b)
            {
                return Compare(aPtr, a.Length, bPtr, b.Length);
            }
        }
    }

    /// <summary>
    /// Compare two datoms
    /// </summary>
    public static int Compare(in Datom a, in Datom b)
    {
        var cmp = ((byte)a.Prefix.Index).CompareTo((byte)b.Prefix.Index);
        if (cmp != 0)
            return cmp;
        
        return a.Prefix.Index switch
        {
            IndexType.TxLog => TxLogComparator.Compare(a, b),
            IndexType.EAVTCurrent or IndexType.EAVTHistory => EAVTComparator.Compare(a, b),
            IndexType.AEVTCurrent or IndexType.AEVTHistory => AEVTComparator.Compare(a, b),
            IndexType.AVETCurrent or IndexType.AVETHistory => AVETComparator.Compare(a, b),
            IndexType.VAETCurrent or IndexType.VAETHistory => VAETComparator.Compare(a, b),
            _ => ThrowArgumentOutOfRangeException()
        };
    }

    private static int ThrowArgumentOutOfRangeException()
    {
        throw new ArgumentOutOfRangeException();
    }

    /// <inheritdoc />
    public unsafe int Compare(byte[]? x, byte[]? y)
    {
        fixed (byte* xPtr = x)
        fixed (byte* yPtr = y)
        {
            return Compare(xPtr, x!.Length, yPtr, y!.Length);
        }
    }
}
