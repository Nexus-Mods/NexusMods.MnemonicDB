using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares the assert part of the datom
/// </summary>
public class AssertComparer<TRegistry>: IElementComparer<TRegistry>
    where TRegistry : IAttributeRegistry
{
    /// <inheritdoc />
    public static int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<KeyPrefix>(a).IsRetract.CompareTo(MemoryMarshal.Read<KeyPrefix>(b).IsRetract);
    }
}
