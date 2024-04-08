using System;
using System.Buffers;

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
    ///     The Unique Id for this type
    /// </summary>
    public Symbol UniqueId { get; }

    /// <summary>
    ///     Compare two spans of bytes that contain the serialized value
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b);
}

/// <summary>
///     Fixed size serializers can also do range lookups and so must implement a comparitor
/// </summary>
public interface IValueSerializer<T> : IValueSerializer
{
    /// <summary>
    ///     Reads from the Buffer returning the number of bytes consumed
    /// </summary>
    public T Read(ReadOnlySpan<byte> buffer);

    /// <summary>
    ///     Returns true if the value is inlined, otherwise false and the inlined
    ///     value contains the length of the blob written to the buffer
    /// </summary>
    public void Serialize<TWriter>(T value, TWriter buffer) where TWriter : IBufferWriter<byte>;
}
