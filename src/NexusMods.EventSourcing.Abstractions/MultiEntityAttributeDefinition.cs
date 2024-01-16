using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NexusMods.EventSourcing.Abstractions.Collections;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A collection of entity links to other entities. Think of this as a one to many FK relationship in a
/// database.
/// </summary>
/// <typeparam name="TOwner"></typeparam>
/// <typeparam name="TOther"></typeparam>
public class MultiEntityAttributeDefinition<TOwner, TOther> : IAttribute<MultiEntityAccumulator<TOther>>
    where TOwner : AEntity<TOwner>, IEntity
    where TOther : AEntity<TOther>
{
    private readonly string _name;

    /// <summary>
    /// A collection of entity links to other entities. Think of this as a one to many FK relationship in a
    /// database.
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TOther"></typeparam>
    public MultiEntityAttributeDefinition(string name)
    {
        _name = name;
    }

    /// <inheritdoc />
    public MultiEntityAccumulator<TOther> CreateAccumulator()
    {
        return new MultiEntityAccumulator<TOther>();
    }

    /// <summary>
    /// Adds a link to the other entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <param name="value"></param>
    /// <typeparam name="TContext"></typeparam>
    public void Add<TContext>(TContext context, EntityId<TOwner> owner, EntityId<TOther> value)
        where TContext : IEventContext
    {
        if (context.GetAccumulator<TOwner, MultiEntityAttributeDefinition<TOwner, TOther>, MultiEntityAccumulator<TOther>>(owner, this, out var accumulator))
            accumulator.Add(value);
    }

    /// <summary>
    /// Adds multiple links to the other entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <param name="value"></param>
    /// <typeparam name="TContext"></typeparam>
    public void AddAll<TContext>(TContext context, EntityId<TOwner> owner, EntityId<TOther>[] values)
        where TContext : IEventContext
    {
        if (context.GetAccumulator<TOwner, MultiEntityAttributeDefinition<TOwner, TOther>, MultiEntityAccumulator<TOther>>(owner, this, out var accumulator))
            foreach (var value in values)
                accumulator.Add(value);
    }

    /// <summary>
    /// Removes a link to the other entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <param name="value"></param>
    /// <typeparam name="TContext"></typeparam>
    public void Remove<TContext>(TContext context, EntityId<TOwner> owner, EntityId<TOther> value)
        where TContext : IEventContext
    {
        if (context.GetAccumulator<TOwner, MultiEntityAttributeDefinition<TOwner, TOther>, MultiEntityAccumulator<TOther>>(owner, this, out var accumulator))
            accumulator.Remove(value);
    }

    /// <summary>
    /// Gets the other entities linked to the given entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public ReadOnlyObservableCollection<TOther> Get(TOwner entity)
    {
        if (!entity.Context
                .GetReadOnlyAccumulator<TOwner, MultiEntityAttributeDefinition<TOwner, TOther>,
                    MultiEntityAccumulator<TOther>>(entity.Id, this, out var accumulator, true))
            throw new InvalidOperationException("No accumulator found for entity");

        accumulator.Init(entity.Context);
        return accumulator.TransformedReadOnly!;
    }

    /// <inheritdoc />
    public Type Owner => typeof(TOwner);

    /// <inheritdoc />
    public string Name => _name;

    IAccumulator IAttribute.CreateAccumulator()
    {
        return CreateAccumulator();
    }
}

/// <inheritdoc />
public class MultiEntityAccumulator<TType> : IAccumulator
    where TType : AEntity<TType>
{
    /// <summary>
    /// The input ids
    /// </summary>
    public readonly ObservableCollection<EntityId<TType>> Ids = new();

    /// <summary>
    /// The transformed values.
    /// </summary>
    public TransformingObservableCollection<EntityId<TType>, TType>? Transformed = null!;

    /// <summary>
    /// The transformed values as a read only collection.
    /// </summary>
    public ReadOnlyObservableCollection<TType>? TransformedReadOnly = null;

    public void Init(IEntityContext context)
    {
        if (Transformed != null)
            return;
        Transformed = new TransformingObservableCollection<EntityId<TType>, TType>(context.Get, Ids);
        TransformedReadOnly = new ReadOnlyObservableCollection<TType>(Transformed);
    }

    /// <summary>
    /// Adds an Entity to the accumulator.
    /// </summary>
    /// <param name="id"></param>
    public void Add(EntityId<TType> id)
    {
        Ids.Add(id);
    }

    /// <summary>
    /// Adds an Entity to the accumulator.
    /// </summary>
    /// <param name="id"></param>
    public void Remove(EntityId<TType> id)
    {
        Ids.Remove(id);
    }

    /// <inheritdoc />
    public void WriteTo(IBufferWriter<byte> writer, ISerializationRegistry registry)
    {
        registry.Serialize(writer, Ids.ToArray());
    }

    /// <inheritdoc />
    public int ReadFrom(ref ReadOnlySpan<byte> reader, ISerializationRegistry registry)
    {
        var read = registry.Deserialize(reader, out EntityId<TType>[] ids);
        Ids.Clear();
        foreach (var id in ids)
            Ids.Add(id);
        return read;
    }
}
