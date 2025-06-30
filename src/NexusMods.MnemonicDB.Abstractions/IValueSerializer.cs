using System;
using System.Buffers;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// An abstract (static) interface for value serializers
/// </summary>
public interface IValueSerializer
{
    /// <summary>
    /// The value tag that this serializer can handle
    /// </summary>
    public static abstract ValueTag ValueTag { get; }
    
    
    /// <summary>
    /// Compare the two values found in the given spans. The spans are guaranteed to be of the same type
    /// </summary>
    public static abstract int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
    
    /// <summary>
    /// Unsafe compare the two values found in the given pointers. The pointers are guaranteed to be of the same type
    /// </summary>
    public static abstract unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen);
}

/// <summary>
/// A typed value serializer for a specific type
/// </summary>
public interface IValueSerializer<T> : IValueSerializer
{
    /// <summary>
    /// Read the value from the given span
    /// </summary>
    public static abstract T Read(ReadOnlySpan<byte> span);

    /// <summary>
    /// Write the value to the given writer
    /// </summary>
    public static abstract void Write<TWriter>(T value, TWriter writer)
        where TWriter : IBufferWriter<byte>;
    
    /// <summary>
    /// Remap any references in the given span using the given remap function
    /// </summary>
    public static abstract void Remap(Span<byte> span, Func<EntityId, EntityId> remapFn);
}

