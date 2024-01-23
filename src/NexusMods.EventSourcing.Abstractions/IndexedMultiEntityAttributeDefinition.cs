using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Like <see cref="MultiEntityAttributeDefinition{TOwner,TOther}"/>, but with the data stored in a dictionary
/// </summary>
public class IndexedMultiEntityAttributeDefinition<TOwner, TKey, TOther>(string name) : IAttribute<IndexedMultiEntityAccumulator<TKey, TOther>>
    where TOwner : AEntity, IEntity
    where TOther : AEntity
    where TKey : notnull
{
    /// <inheritdoc />
    public Type Owner => typeof(TOwner);

    /// <inheritdoc />
    public string Name => name;

    /// <inheritdoc />
    public IndexedMultiEntityAccumulator<TKey, TOther> CreateAccumulator()
    {
        return new IndexedMultiEntityAccumulator<TKey, TOther>();
    }

    /// <summary>
    /// Adds a new entity to the collection.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="TContext"></typeparam>
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

/// <summary>
/// Accumulator for the <see cref="IndexedMultiEntityAttributeDefinition{TOwner,TKey,TOther}"/> attribute.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TOther"></typeparam>
public class IndexedMultiEntityAccumulator<TKey, TOther> : IAccumulator
    where TOther : AEntity
    where TKey : notnull
{
    internal Dictionary<TKey, EntityId<TOther>> _values = new();
    internal Dictionary<EntityId<TOther>, TKey> _keys = new();

    /// <inheritdoc />
    public void WriteTo(IBufferWriter<byte> writer, ISerializationRegistry registry)
    {
        registry.Serialize(writer, _keys.Keys.ToArray());
        registry.Serialize(writer, _keys.Values.ToArray());
    }

    /// <inheritdoc />
    public int ReadFrom(ref ReadOnlySpan<byte> input, ISerializationRegistry registry)
    {
        var keySize = registry.Deserialize<EntityId<TOther>[]>(input, out var keys);
        input = input.Slice(keySize);
        var valueSize = registry.Deserialize<TKey[]>(input, out var values);

        for (var i = 0; i < keys.Length; i++)
        {
            _keys.Add(keys[i], values[i]);
            _values.Add(values[i], keys[i]);
        }


        return keySize + valueSize;
    }
}
