using System;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// Abstract datom comparator, compares the A, B, C, D and E parts of the datom, in that order
/// </summary>
public abstract unsafe class ADatomComparator<TA, TB, TC, TD> : IDatomComparator
    where TA : IElementComparer
    where TB : IElementComparer
    where TC : IElementComparer
    where TD : IElementComparer
{
    

    /// <inheritdoc />
    public static int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TD.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TD.Compare(aPtr, aLen, bPtr, bLen);
    }

    /// <inheritdoc />
    public static int Compare(in Datom a, in Datom b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(a, b);
        if (cmp != 0) return cmp;

        return TD.Compare(a, b);
    }

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(a, b);
        if (cmp != 0) return cmp;

        return TD.Compare(a, b);
    }

    /// <inheritdoc />
    public int CompareInstance(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TD.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
    }

    /// <inheritdoc />
    public int CompareInstance(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TD.Compare(aPtr, aLen, bPtr, bLen);
    }

    /// <inheritdoc />
    public int CompareInstance(in Datom a, in Datom b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(a, b);
        if (cmp != 0) return cmp;

        return TD.Compare(a, b);
    }

    /// <inheritdoc />
    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(a, b);
        if (cmp != 0) return cmp;

        return TD.Compare(a, b);
    }
}
