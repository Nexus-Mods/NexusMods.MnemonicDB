using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// Abstract datom comparator, compares the A, B, C, D and E parts of the datom, in that order
/// </summary>
public abstract unsafe class ADatomComparator<TA, TB, TC, TD, TE> : IDatomComparator
    where TA : IElementComparer
    where TB : IElementComparer
    where TC : IElementComparer
    where TD : IElementComparer
    where TE : IElementComparer
{
    public static int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var cmp = TA.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        cmp = TD.Compare(aPtr, aLen, bPtr, bLen);
        if (cmp != 0) return cmp;

        return TE.Compare(aPtr, aLen, bPtr, bLen);
    }

    /// <inheritdoc />
    public int CompareInstance(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return Compare(aPtr, aLen, bPtr, bLen);
    }
}
