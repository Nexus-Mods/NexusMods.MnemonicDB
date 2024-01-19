using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marks an attribute defintion as indexable. Indexable attributes take part of a composite index, and are used to
/// look up entities by their attributes. The most common example of this is the EntityId, which is used to look up
/// entities by their Id. Indexable attributes must be immutable (once set they are set forever), and the usages
/// of them must be marked with <see cref="IndexedAttribute"/>.
///
/// Indexed attributes much be serializable to a constant size value, but the value need not be unique. For variable
/// length values, hashing them and using the hash as the index is recommended. Once the entity is loaded with the help
/// of the index, the actual value can be compared to ensure the values are correct.
/// </summary>
public interface IIndexableAttribute : IAttribute
{
    /// <summary>
    /// The Id of the attribute definition index.
    /// </summary>
    public UInt128 IndexedAttributeId { get; set; }

    /// <summary>
    /// The size of the indexed attribute values in bytes. These must be a fixed size.
    /// </summary>
    /// <returns></returns>
    public int SpanSize();


    /// <summary>
    /// Writes the accumulator to the given span, which will be the size returned by <see cref="SpanSize"/>.
    /// </summary>
    /// <param name="span"></param>
    /// <param name="accumulator"></param>
    public void WriteTo(Span<byte> span, IAccumulator accumulator);
}


/// <summary>
/// A Typed version of <see cref="IIndexableAttribute"/>, which allows serialization of a single value. This is
/// used when constructing a index key for reading from the index.
/// </summary>
/// <typeparam name="TVal"></typeparam>
public interface IIndexableAttribute<TVal> : IIndexableAttribute
{
    /// <summary>
    /// Writes the accumulator to the given span, which will be the size returned by <see cref="IIndexableAttribute.SpanSize"/>.
    /// </summary>
    /// <param name="span"></param>
    /// <param name="value"></param>
    public void WriteTo(Span<byte> span, TVal value);

    /// <summary>
    /// Returns true if the accumulator value is equal to the given value, used for the final filter
    /// from secondary indexes.
    /// </summary>
    /// <param name="accumulator"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    bool Equal(IAccumulator accumulator, TVal val);
}
