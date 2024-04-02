using System;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares to elements of a datom. We use this and generics to abuse the inlining
/// of the compiler to generate efficient comparison code.
/// </summary>
public interface IElementComparer<in TAttributeRegistry>
    where TAttributeRegistry : IAttributeRegistry
{
    /// <summary>
    /// Compares two elements of a datom.
    /// </summary>
    public static abstract int Compare(TAttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
