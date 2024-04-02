using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.ElementComparers;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

/// <summary>
/// Compares the Tx part of the key.
/// </summary>
public class TxComparer<TRegistry> : IElementComparer<TRegistry>
    where TRegistry : IAttributeRegistry
{
    /// <inheritdoc />
    public static int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<KeyPrefix>(a).T.CompareTo(MemoryMarshal.Read<KeyPrefix>(b).T);
    }
}
