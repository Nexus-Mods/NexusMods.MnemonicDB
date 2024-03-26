using System;
using System.Buffers;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A registry of attributes and serializers that supports operations that requires converting
/// between the database IDs, the code-level attributes and the native values
/// </summary>
public interface IAttributeRegistry
{
    /// <summary>
    /// Compares the given values in the given spans assuming both are tagged with the given attribute
    /// </summary>
    public int CompareValues(AttributeId id, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);

    /// <summary>
    /// Sets the attribute id and value in the given datom based on the given attribute and value
    /// </summary>
    void Explode<TAttribute, TValueType, TBufferWriter>(out AttributeId attrId, TValueType valueType, TBufferWriter writer)
        where TBufferWriter : IBufferWriter<byte>
        where TAttribute : IAttribute<TValueType>;
}
