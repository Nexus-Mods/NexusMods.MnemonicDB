using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

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
    public static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TC.Compare(aPtr, aLen, bPtr, bLen);
    }

    /// <summary>
    /// Compare two datom spans
    /// </summary>
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        fixed(byte* aPtr = a)
        fixed(byte* bPtr = b)
            return Compare(aPtr, a.Length, bPtr, b.Length);
    }

    /// <summary>
    /// Compare two datoms
    /// </summary>
    public static int Compare(in Datom a, in Datom b)
    {
        return Compare(a.RawSpan, b.RawSpan);
    }

    /// <inheritdoc />
    public int CompareInstance(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return Compare(aPtr, aLen, bPtr, bLen);
    }
}
