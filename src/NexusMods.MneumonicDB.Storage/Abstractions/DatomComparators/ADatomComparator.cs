﻿using System;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Storage.Abstractions.DatomComparators;

public abstract class ADatomComparator<TA, TB, TC, TD, TE, TRegistry> : IDatomComparator<TRegistry>
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
}
