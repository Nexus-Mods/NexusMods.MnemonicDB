﻿using System;

namespace NexusMods.MnemonicDB.Abstractions.DatomComparators;

/// <summary>
/// A comparator for datoms
/// </summary>
public unsafe interface IDatomComparator
{
    /// <summary>
    /// Compare two datoms
    /// </summary>
    public static abstract int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen);

    /// <summary>
    /// Compare two datoms
    /// </summary>
    public int CompareInstance(byte* aPtr, int aLen, byte* bPtr, int bLen);

    /// <summary>
    /// Compare two datoms
    /// </summary>
    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        fixed(byte* aPtr = a)
        fixed(byte* bPtr = b)
            return CompareInstance(aPtr, a.Length, bPtr, b.Length);
    }
}
