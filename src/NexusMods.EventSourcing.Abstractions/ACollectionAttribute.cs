using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Abstract base class for an attribute that is a collection of entities.
/// </summary>
/// <param name="name"></param>
/// <typeparam name="TOwner"></typeparam>
/// <typeparam name="TType"></typeparam>
public class ACollectionAttribute<TOwner, TType>(string name) : IAttribute
where TOwner : IEntity
{
    /// <inheritdoc />
    public Type Owner => typeof(TOwner);

    /// <inheritdoc />
    public string Name => name;

    /// <inheritdoc />
    public IAccumulator CreateAccumulator()
    {
        return new Accumulator();
    }

    /// <inheritdoc />
    private class Accumulator : IAccumulator
    {
        private HashSet<TType> _values = new();
        public void Add(object value)
        {
            _values.Add((TType) value);
        }

        public void Retract(object value)
        {
            _values.Remove((TType) value);
        }

        public object Get()
        {
            return _values;
        }

        /// <inheritdoc />
        public void WriteTo(IBufferWriter<byte> writer, ISerializationRegistry registry)
        {
            registry.Serialize(writer, _values.ToArray());
        }

        /// <inheritdoc />
        public int ReadFrom(ref ReadOnlySpan<byte> reader, ISerializationRegistry registry)
        {
            var read = registry.Deserialize(reader, out TType[] values);
            _values = [..values];
            return read;
        }
    }

    /// <summary>
    /// The type of the items in the collection.
    /// </summary>
    public Type Type => typeof(TType);
}
