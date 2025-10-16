using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;

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
    /// Compares the elements from two datoms.
    /// </summary>
    public static abstract int Compare(in Datom a, in Datom b);

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
    /// Compares the elements from two datoms.
    /// </summary>
    public int CompareInstance(in Datom a, in Datom b);

    public int CompareInstance<TD1, TD2>(in TD1 d1, in TD2 d2)
        where TD1 : IDatomLikeRO, allows ref struct
        where TD2 : IDatomLikeRO, allows ref struct;

    public static abstract int Compare<TD1, TD2>(in TD1 d1, in TD2 d2)
        where TD1 : IDatomLikeRO, allows ref struct
        where TD2 : IDatomLikeRO, allows ref struct;

    

    /// <summary>
    /// Compare two datom spans
    /// </summary>
    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
