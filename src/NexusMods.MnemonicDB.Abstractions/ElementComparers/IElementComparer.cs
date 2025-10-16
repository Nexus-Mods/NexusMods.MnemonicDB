﻿using System;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares to elements of a datom. We use this and generics to abuse the inlining
/// of the compiler to generate efficient comparison code.
/// </summary>
public interface IElementComparer
{
    /// <summary>
    /// Compares two elements of a datom from the given pointers
    /// </summary>
    public static abstract unsafe int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen);

    /// <summary>
    /// Compares two elements of a datom from the given pointers
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static abstract unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen);

    /// <summary>
    /// Compares the elements from two datoms.
    /// </summary>
    public static abstract int Compare(in Datom a, in Datom b);

    /// <summary>
    /// Compare two datom spans
    /// </summary>
    public static abstract int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
    
    public static abstract int Compare<T1, T2>(in T1 a, in T2 b)
        where T1 : IDatomLikeRO, allows ref struct
        where T2 : IDatomLikeRO, allows ref struct;
}
