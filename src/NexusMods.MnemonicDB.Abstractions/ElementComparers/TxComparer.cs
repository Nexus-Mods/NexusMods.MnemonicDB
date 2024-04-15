using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

/// <summary>
/// Compares the Tx part of the key.
/// </summary>
public class TxComparer : IElementComparer
{
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return IElementComparer.KeyPrefix(aPtr)->T.CompareTo(IElementComparer.KeyPrefix(bPtr)->T);
    }
}
