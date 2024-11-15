using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Iterators;

/// <summary>
/// A slice descriptor with no value.
/// </summary>
public readonly ref struct NullRefSliceDescriptor : IRefSliceDescriptor
{
    private readonly KeyPrefix _lower;
    private readonly KeyPrefix _upper;
    /// <summary>
    /// A slice descriptor with no values
    /// </summary>
    public NullRefSliceDescriptor(KeyPrefix low, KeyPrefix high)
    {
        _lower = low;
        _upper = high;
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> LowerBound => 
        MemoryMarshal.CreateReadOnlySpan(in _lower, 1).CastFast<KeyPrefix, byte>();

    /// <inheritdoc />
    public ReadOnlySpan<byte> UpperBound =>
        MemoryMarshal.CreateReadOnlySpan(in _upper, 1).CastFast<KeyPrefix, byte>();
}
