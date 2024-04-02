using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the E part of the key.
/// </summary>
public class EComparer<TRegistry> : IElementComparer<TRegistry>
    where TRegistry : IAttributeRegistry
{
    /// <inheritdoc />
    public static int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<KeyPrefix>(a).E.CompareTo(MemoryMarshal.Read<KeyPrefix>(b).E);
    }
}
