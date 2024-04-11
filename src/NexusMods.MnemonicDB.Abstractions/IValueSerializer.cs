using System;
using System.Buffers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A abstract interface for a value serializer. Serializers can either emit fixed
///     or variable sized segments.
/// </summary>
public interface IValueSerializer
{
    /// <summary>
    ///     The native .NET type for this serializer
    /// </summary>
    public Type NativeType { get; }

    /// <summary>
    /// The type as stored in the datom store
    /// </summary>
    public LowLevelTypes LowLevelType { get; }

    /// <summary>
    ///     The Unique Id for this serializer
    /// </summary>
    public Symbol UniqueId { get; }
}

/// <summary>
///     Fixed size serializers can also do range lookups and so must implement a comparitor
/// </summary>
public interface IValueSerializer<T> : IValueSerializer
{
    /// <summary>
    ///     Reads the value from the buffer, the prefix is expected to be pre-populated with the values from the span
    /// </summary>
    public T Read(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan);

    /// <summary>
    ///     Serializes the value to the buffer, and sets the prefix values for low level type and length. This method
    /// is expected to write the prefix & and the value to the buffer.
    /// </summary>
    public void Serialize<TWriter>(ref KeyPrefix prefix, T value, TWriter buffer) where TWriter : IBufferWriter<byte>;
}
