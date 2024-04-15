using System;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// A comparator for datoms
/// </summary>
public interface IDatomComparator
{
    /// <summary>
    /// Compare two datoms
    /// </summary>
    public static abstract int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);

    /// <summary>
    /// Compare two datoms
    /// </summary>
    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
