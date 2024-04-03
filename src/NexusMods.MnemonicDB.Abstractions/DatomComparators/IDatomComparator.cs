using System;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.DatomIterators;

/// <summary>
/// A comparator for datoms
/// </summary>
public interface IDatomComparator<in TRegistry>
    where TRegistry : IAttributeRegistry
{
    /// <summary>
    /// Compare two datoms
    /// </summary>
    public static abstract int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);

    /// <summary>
    /// Compare two datoms
    /// </summary>
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
