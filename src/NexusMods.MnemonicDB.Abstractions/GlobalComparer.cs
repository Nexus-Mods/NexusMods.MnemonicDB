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
    private const ulong IndexMask = 0xFFUL << 40;
    /// <summary>
    /// Compare two byte arrays that are prefixed by the index type
    /// </summary>
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var prefixA = (KeyPrefix*)aPtr;
        var prefixB = (KeyPrefix*)bPtr;
        
        var aIndex = prefixA->Upper & IndexMask;
        var bIndex = prefixB->Upper & IndexMask;
        
        var cmp = aIndex.CompareTo(bIndex);
        if (cmp != 0)
            return cmp;

        return (IndexType)(aIndex >> 40)  switch
        {
            IndexType.EAVTCurrent or IndexType.EAVTHistory => EAVTComparator.Compare(aPtr, aLen, bPtr, bLen),
            IndexType.AEVTCurrent or IndexType.AEVTHistory => AEVTComparator.Compare(aPtr, aLen, bPtr, bLen),
            IndexType.AVETCurrent or IndexType.AVETHistory => AVETComparator.Compare(aPtr, aLen, bPtr, bLen),
            IndexType.VAETCurrent or IndexType.VAETHistory => VAETComparator.Compare(aPtr, aLen, bPtr, bLen),
            IndexType.TxLog => TxLogComparator.Compare(aPtr, aLen, bPtr, bLen),
            _ => -1,
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

    /// <summary>
    /// Compare two pointers, remapping history indices to current indices
    /// </summary>
    public static unsafe int CompareIgnoreHistoryIndex(Ptr leftKey, Ptr rightKey)
    {
        var prefixA = (KeyPrefix*)leftKey.Base;
        var prefixB = (KeyPrefix*)rightKey.Base;

        var remappedIndexA = prefixA->Index
            switch
            {
                IndexType.EAVTHistory => IndexType.EAVTCurrent,
                IndexType.AEVTHistory => IndexType.AEVTCurrent,
                IndexType.AVETHistory => IndexType.AVETCurrent,
                IndexType.VAETHistory => IndexType.VAETCurrent,
                _ => prefixA->Index
            };
        
        var remappedIndexB = prefixB->Index
            switch
            {
                IndexType.EAVTHistory => IndexType.EAVTCurrent,
                IndexType.AEVTHistory => IndexType.AEVTCurrent,
                IndexType.AVETHistory => IndexType.AVETCurrent,
                IndexType.VAETHistory => IndexType.VAETCurrent,
                _ => prefixB->Index
            };
        
        var cmp = (remappedIndexA).CompareTo(remappedIndexB);
        if (cmp != 0)
            return cmp;
        
        var aPtr = leftKey.Base;
        var aLen = leftKey.Length;
        var bPtr = rightKey.Base;
        var bLen = rightKey.Length;

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
}
