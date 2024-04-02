using System;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

/// <summary>
/// A comparator for datoms
/// </summary>
public interface IDatomComparator
{
    /// <summary>
    /// Compare two datoms
    /// </summary>
    public static abstract int Compare(IAttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
