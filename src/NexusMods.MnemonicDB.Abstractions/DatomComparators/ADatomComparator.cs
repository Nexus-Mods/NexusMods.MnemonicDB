using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// Abstract datom comparator, compares the A, B, C, D and E parts of the datom, in that order
/// </summary>
public abstract class ADatomComparator<TA, TB, TC, TD, TE> : IDatomComparator
    where TA : IElementComparer
    where TB : IElementComparer
    where TC : IElementComparer
    where TD : IElementComparer
    where TE : IElementComparer
{
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var cmp = TA.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(a, b);
        if (cmp != 0) return cmp;

        cmp = TD.Compare(a, b);
        if (cmp != 0) return cmp;

        return TE.Compare(a, b);
    }

    /// <inheritdoc />
    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Compare(a, b);
    }
}
