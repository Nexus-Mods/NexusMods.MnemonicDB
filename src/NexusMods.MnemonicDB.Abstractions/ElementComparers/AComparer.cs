using System;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the A part of the key.
/// </summary>
public sealed class AComparer : IElementComparer
{
    /// <inheritdoc />
    public static unsafe int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        return aPrefix->A.CompareTo(bPrefix->A);
    }

    private const ulong AMask = 0xFFFF000000000000UL;
    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var aVal = ((KeyPrefix*)aPtr)->Upper & AMask;
        var bVal = ((KeyPrefix*)bPtr)->Upper & AMask;
        // Use simple if/else to compare.
        return aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
    }

    /// <inheritdoc />
    public static int Compare(in Datom a, in Datom b)
    {
        return a.A.CompareTo(b.A);
    }

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = KeyPrefix.Read(a);
        var keyB = KeyPrefix.Read(b);
        return keyA.A.CompareTo(keyB.A);
    }
}
