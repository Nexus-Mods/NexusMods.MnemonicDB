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
    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<KeyPrefix>(a).T.CompareTo(MemoryMarshal.Read<KeyPrefix>(b).T);
    }
}
