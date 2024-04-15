using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the E part of the key.
/// </summary>
public class EComparer : IElementComparer
{
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return IElementComparer.KeyPrefix(aPtr)->E.CompareTo(IElementComparer.KeyPrefix(bPtr)->E);
    }
}
