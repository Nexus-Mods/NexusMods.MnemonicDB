﻿using System;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

/// <summary>
/// A comparator for datoms
/// </summary>
public interface IDatomComparator<in TRegistry>
    where TRegistry : IAttributeRegistry
{
    /// <summary>
    /// Compare two datoms
    /// </summary>
    public static abstract int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
