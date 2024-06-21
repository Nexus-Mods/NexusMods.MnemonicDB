using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// A comparator that only considers the EAV portion of the datom, useful for in-memory sets that
/// are not concerned with time, and only contain asserts
/// </summary>
public abstract unsafe class APartialDatomComparator<TA, TB, TC> : IDatomComparator
    where TA : IElementComparer
    where TB : IElementComparer
    where TC : IElementComparer
{
    /// <inheritdoc />
    public static int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TC.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
    }

    /// <inheritdoc />
    public static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TC.Compare(aPtr, aLen, bPtr, bLen);
    }

    /// <inheritdoc />
    public static int Compare(in Datom a, in Datom b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        return TC.Compare(a, b);
    }

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        return TC.Compare(a, b);
    }

    /// <inheritdoc />
    public int CompareInstance(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TC.Compare(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
    }

    /// <inheritdoc />
    public int CompareInstance(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TC.Compare(aPtr, aLen, bPtr, bLen);
    }

    /// <inheritdoc />
    public int CompareInstance(in Datom a, in Datom b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        return TC.Compare(a, b);
    }

    /// <inheritdoc />
    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        return TC.Compare(a, b);
    }
}
