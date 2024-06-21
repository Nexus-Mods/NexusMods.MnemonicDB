using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the A part of the key.
/// </summary>
public class AComparer : IElementComparer
{
    /// <inheritdoc />
    public static unsafe int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        return aPrefix->A.CompareTo(bPrefix->A);
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return ((KeyPrefix*)aPtr)->A.CompareTo(((KeyPrefix*)bPtr)->A);
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
