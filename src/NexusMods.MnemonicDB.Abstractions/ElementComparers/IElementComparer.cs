using System;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Compares to elements of a datom. We use this and generics to abuse the inlining
/// of the compiler to generate efficient comparison code.
/// </summary>
public interface IElementComparer
{
    /// <summary>
    /// Compares two elements of a datom.
    /// </summary>
    public static abstract int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
