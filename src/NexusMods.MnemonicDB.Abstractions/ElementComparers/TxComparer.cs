using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

/// <summary>
/// Compares the Tx part of the key.
/// </summary>
public sealed class TxComparer : IElementComparer
{
    /// <inheritdoc />
    public static unsafe int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        return aPrefix->T.CompareTo(bPrefix->T);
    }

    /// <inheritdoc />
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return ((KeyPrefix*)aPtr)->T.CompareTo(((KeyPrefix*)bPtr)->T);
    }

    /// <inheritdoc />
    public static int Compare(in Datom a, in Datom b)
    {
        return a.T.CompareTo(b.T);
    }

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = KeyPrefix.Read(a);
        var keyB = KeyPrefix.Read(b);
        return keyA.T.CompareTo(keyB.T);
    }

    /// <inheritdoc />
    public static int Compare<T1, T2>(in T1 a, in T2 b) 
        where T1 : IDatomLikeRO, allows ref struct 
        where T2 : IDatomLikeRO, allows ref struct
    {
        return a.T.CompareTo(b.T);
    }
}
