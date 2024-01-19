using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A internal class used for creating secondary indexes on entity Ids,
/// </summary>
public class EntityIdDefinition : IAttribute<EntityIdDefinitionAccumulator>, IIndexableAttribute<EntityId>
{
    /// <inheritdoc />
    public Type Owner => typeof(IEntity);

    /// <inheritdoc />
    public string Name => "Id";

    /// <inheritdoc />
    public EntityIdDefinitionAccumulator CreateAccumulator()
    {
        return new EntityIdDefinitionAccumulator();
    }

    IAccumulator IAttribute.CreateAccumulator()
    {
        return new EntityIdDefinitionAccumulator();
    }

    /// <inheritdoc />
    public void WriteTo(Span<byte> span, EntityId value)
    {
        BinaryPrimitives.WriteUInt128BigEndian(span, value.Value);
    }

    /// <inheritdoc />
    public bool Equal(IAccumulator accumulator, EntityId val)
    {
        return ((EntityIdDefinitionAccumulator)accumulator).Id == val;
    }

    private static readonly UInt128 IndexAttrId = UInt128.Parse("6a434d5d732e40278d0e43385482368d", NumberStyles.HexNumber);

    /// <inheritdoc />
    public UInt128 IndexedAttributeId
    {
        get => IndexAttrId;
        set => throw new InvalidOperationException("Can't set the indexed attribute id.");
    }

    /// <inheritdoc />
    public int SpanSize()
    {
        return 16;
    }

    /// <inheritdoc />
    public void WriteTo(Span<byte> span, IAccumulator accumulator)
    {
        if (accumulator is not EntityIdDefinitionAccumulator entityDefinitionAccumulator)
            throw new InvalidOperationException("Invalid accumulator type.");

        BinaryPrimitives.WriteUInt128BigEndian(span, entityDefinitionAccumulator.Id.Value);
    }
}

/// <summary>
/// Accumulator for the <see cref="EntityIdDefinition"/> attribute.
/// </summary>
public class EntityIdDefinitionAccumulator : IAccumulator
{
    /// <summary>
    /// The Id of the entity.
    /// </summary>
    public EntityId Id;

    /// <inheritdoc />
    public void WriteTo(IBufferWriter<byte> writer, ISerializationRegistry registry)
    {
        var span = writer.GetSpan(16);
        BinaryPrimitives.WriteUInt128BigEndian(span, Id.Value);
        writer.Advance(16);
    }

    /// <inheritdoc />
    public int ReadFrom(ref ReadOnlySpan<byte> data, ISerializationRegistry registry)
    {
        Id = EntityId.From(data);
        return 16;
    }

    /// <summary>
    /// Creates a new accumulator with the Id set
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityIdDefinitionAccumulator From(EntityId id)
    {
        return new EntityIdDefinitionAccumulator { Id = id };
    }
}
