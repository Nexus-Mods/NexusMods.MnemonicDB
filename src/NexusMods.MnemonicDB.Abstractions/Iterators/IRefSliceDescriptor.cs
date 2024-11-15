using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Iterators;

/// <summary>
/// An interface for implementing a slice descriptor for a reference slice.
/// </summary>
public interface IRefSliceDescriptor
{
    /// <summary>
    /// The lower bound of the slice.
    /// </summary>
    public ReadOnlySpan<byte> LowerBound { get; }
    
    /// <summary>
    /// The upper bound of the slice.
    /// </summary>
    public ReadOnlySpan<byte> UpperBound { get; }
}
