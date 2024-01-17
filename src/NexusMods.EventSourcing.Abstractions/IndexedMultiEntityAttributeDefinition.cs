using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Like <see cref="MultiEntityAttributeDefinition{TOwner,TOther}"/>, but with the data stored in a dictionary
/// </summary>
public class IndexedMultiEntityAttributeDefinition<TOwner, TKey, TOther>(string name) : IAttribute<IndexedMultiEntityAccumulator<TKey, TOther>>
    where TOwner : AEntity<TOwner>, IEntity
    where TOther : AEntity<TOther>
    where TKey : notnull
{
    /// <inheritdoc />
    public Type Owner => typeof(TOwner);

    /// <inheritdoc />
    public string Name => name;

    public IndexedMultiEntityAccumulator<TKey, TOther> CreateAccumulator()
    {
        return new IndexedMultiEntityAccumulator<TKey, TOther>();
    }

    public void Add<TContext>(TContext context, EntityId<TOwner> owner, TKey key, EntityId<TOther> value)
        where TContext : IEventContext
    {
        if (context.GetAccumulator<TOwner, IndexedMultiEntityAttributeDefinition<TOwner, TKey, TOther>,
                IndexedMultiEntityAccumulator<TKey, TOther>>(owner, this, out var accumulator))
        {
            if (accumulator._keys.TryGetValue(value, out var existingKey))
            {
                accumulator._values.Remove(existingKey);
                accumulator._keys.Remove(value);
            }
            accumulator._values.Add(key, value);
            accumulator._keys.Add(value, key);
        }
    }

    IAccumulator IAttribute.CreateAccumulator()
    {
        return CreateAccumulator();
    }

    /// <summary>
    /// Gets the other entities linked to the given entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public Dictionary<TKey, EntityId<TOther>> Get(TOwner entity)
    {
        if (!entity.Context
                .GetReadOnlyAccumulator<TOwner, IndexedMultiEntityAttributeDefinition<TOwner, TKey, TOther>,
                    IndexedMultiEntityAccumulator<TKey, TOther>>(entity.Id, this, out var accumulator, true))
            throw new InvalidOperationException("No accumulator found for entity");
        return accumulator._values;
    }
}

public class IndexedMultiEntityAccumulator<TKey, TOther> : IAccumulator
    where TOther : AEntity<TOther>
    where TKey : notnull
{
    internal Dictionary<TKey, EntityId<TOther>> _values = new();
    internal Dictionary<EntityId<TOther>, TKey> _keys = new();

    public void WriteTo(IBufferWriter<byte> writer, ISerializationRegistry registry)
    {
        var getSpan = writer.GetSpan(2);
        BinaryPrimitives.WriteUInt16BigEndian(getSpan, (ushort) _values.Count);
        writer.Advance(2);

        foreach (var (key, value) in _values)
        {
            registry.Serialize(writer, key);
            registry.Serialize(writer, value);
        }
    }

    public int ReadFrom(ref ReadOnlySpan<byte> input, ISerializationRegistry registry)
    {
        var originalSize = input.Length;
        var data = input;

        var count = BinaryPrimitives.ReadUInt16BigEndian(data);
        data = data.Slice(sizeof(ushort));

        for (var idx = 0; idx < count; idx++)
        {
            var written = registry.Deserialize(data, out TKey key);
            data = data.Slice(written);
            written = registry.Deserialize(data, out EntityId<TOther> value);
            data = data.Slice(written);
            _values.Add(key, value);
            _keys.Add(value, key);
        }

        return originalSize - data.Length;
    }
}
