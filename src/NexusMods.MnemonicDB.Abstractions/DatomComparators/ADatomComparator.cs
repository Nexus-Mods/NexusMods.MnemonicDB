using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// Abstract datom comparator, compares the A, B, C, D and E parts of the datom, in that order
/// </summary>
public abstract class ADatomComparator<TA, TB, TC, TD, TE, TRegistry>(TRegistry registry)
    : IDatomComparator<TRegistry>
    where TA : IElementComparer<TRegistry>
    where TB : IElementComparer<TRegistry>
    where TC : IElementComparer<TRegistry>
    where TD : IElementComparer<TRegistry>
    where TE : IElementComparer<TRegistry>
    where TRegistry : IAttributeRegistry
{
    public static int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var cmp = TA.Compare(registry, a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(registry, a, b);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(registry, a, b);
        if (cmp != 0) return cmp;

        cmp = TD.Compare(registry, a, b);
        if (cmp != 0) return cmp;

        return TE.Compare(registry, a, b);
    }

    /// <inheritdoc />
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Compare(registry, a, b);
    }
}
