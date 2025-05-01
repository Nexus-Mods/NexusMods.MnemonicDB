﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the E part of the key.
/// </summary>
public sealed class EComparer : IElementComparer
{
    /// <inheritdoc />
    public static unsafe int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        return aPrefix->E.CompareTo(bPrefix->E);
    }
    
    private const ulong EMask = 0xFF00FFFFFFFFFFFFUL;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var aVal = ((KeyPrefix*)aPtr)->Lower & EMask;
        var bVal = ((KeyPrefix*)bPtr)->Lower & EMask;
        return aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
    }

    /// <inheritdoc />
    public static int Compare(in Datom a, in Datom b)
    {
        return a.E.CompareTo(b.E);
    }

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = KeyPrefix.Read(a);
        var keyB = KeyPrefix.Read(b);
        return keyA.E.CompareTo(keyB.E);
    }
}
