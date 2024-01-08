using System;
using System.Linq.Expressions;

namespace NexusMods.EventSourcing.Abstractions.Serialization;

/// <summary>
/// A typed value serializer for a static size value.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IStaticValueSerializer<T> : IValueSerializer
{
    /// <summary>
    /// Deserialize the value from the given span and return the value.
    /// </summary>
    /// <param name="span"></param>
    /// <param name="value"></param>
    public void Deserialize(ref Span<byte> span, ref T value);

    /// <summary>
    /// Serialize the value into the given span.
    /// </summary>
    /// <param name="span"></param>
    /// <param name="value"></param>
    public void Serialize(ref Span<byte> span, ref T value);

    /// <summary>
    /// Since the size is static, this gets the fixed size of the serialized value.
    /// </summary>
    /// <returns></returns>
    public int GetSize();
}
