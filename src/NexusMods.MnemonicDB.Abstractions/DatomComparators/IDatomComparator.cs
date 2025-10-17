using System;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// A comparator for datoms
/// </summary>
public unsafe interface IDatomComparator
{

    /// <summary>
    /// Compares two elements of a datom from the given pointers
    /// </summary>
    public static abstract int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen);

    /// <summary>
    /// Compares two elements of a datom from the given pointers
    /// </summary>
    public static abstract int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen);
    
    /// <summary>
    /// Compare two datom spans
    /// </summary>
    public static abstract int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);

    /// <summary>
    /// Compares two elements of a datom from the given pointers
    /// </summary>
    public int CompareInstance(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen);

    /// <summary>
    /// Compares two elements of a datom from the given pointers
    /// </summary>
    public int CompareInstance(byte* aPtr, int aLen, byte* bPtr, int bLen);
    
    /// <summary>
    /// Compare two datom spans
    /// </summary>
    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
