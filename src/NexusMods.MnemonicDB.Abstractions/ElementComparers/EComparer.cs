using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the E part of the key.
/// </summary>
public class EComparer : IElementComparer
{
    /// <inheritdoc />
    public static unsafe int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        return aPrefix->E.CompareTo(bPrefix->E);
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return ((KeyPrefix*)aPtr)->E.CompareTo(((KeyPrefix*)bPtr)->E);
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
