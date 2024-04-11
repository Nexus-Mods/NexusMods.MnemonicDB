using System;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.DatomIterators;

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
    /// Instance version of the compare method
    /// </summary>
    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
